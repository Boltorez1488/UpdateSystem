<template>
    <div>
        <b-breadcrumb>
            <b-breadcrumb-item @click="rootClick" :active="this.$root.dir.length === 0">Root</b-breadcrumb-item>
            <b-breadcrumb-item v-for="b in breadcrumbs" :key="b.path"
                               :active="b.isCurrent"
                               @click="breadClick(b.path)">
                {{b.name}}
            </b-breadcrumb-item>
        </b-breadcrumb>

        <b-modal ref="folder-adder" v-model="$root.folderAdder" hide-footer title="Добавление файлов">
            <div class="d-block text-center">
                <b-overlay :show="!$root.locked" rounded="sm">
                    <!--<div id="total-progress">
                        <div id="total-bar" :style="{width: uploadProgress + '%'}"></div>
                    </div>-->
                    <file-pond name="file" ref="pond" class-name="uploader" :server="server"
                            :label-idle="labelIdle"
                            allow-multiple="true" chunk-uploads="true" :chunk-size="20 * 1024 * 1024" />

                    <!--<vue-dropzone ref="dropzone" id="dropzone" v-on:vdropzone-sending="sendingEvent"
                                  @vdropzone-upload-progress="dropzoneUploadProgress"
                                  @vdropzone-file-added="dropzoneFileAdded"
                                  @vdropzone-success="uploadSuccess" :options="dropzoneOptions" :useCustomSlot=true>
                        <div class="dropzone-custom-content">
                            <h3 class="dropzone-custom-title">Перетащите файлы для загрузки!</h3>
                            <div class="subtitle">...или выберите их руками после нажатия</div>
                        </div>
                    </vue-dropzone>-->
                </b-overlay>
            </div>
        </b-modal>

        <b-modal ref="remover-creator" hide-footer title="Создание удаления">
          <div class="d-block text-center">
            <b-form-input v-model="removerName" placeholder="Имя"/>
          </div>
          <b-button class="mt-2" variant="outline-warning" block @click="createRemover">Создать</b-button>
        </b-modal>

        <b-modal ref="folder-creator" hide-footer title="Создание папки">
            <div class="d-block text-center">
                <b-form-input v-model="folderCreate" placeholder="Папка"/>
            </div>
            <b-button class="mt-2" variant="outline-warning" block @click="createFolder">Создать</b-button>
        </b-modal>

        <b-modal ref="folder-renamer" hide-footer>
            <template v-slot:modal-title>
                Переименование папки: {{folderRenameOri}}
            </template>
            <div class="d-block text-center">
                <b-form-input v-model="folderRename" placeholder="Имя"/>
            </div>
            <b-button class="mt-2" variant="outline-warning" block @click="folderRenamerOk">Применить</b-button>
        </b-modal>

        <b-modal ref="file-renamer" hide-footer>
            <template v-slot:modal-title>
                Переименование файла: {{fileRenameOri}}
            </template>
            <div class="d-block text-center">
                <b-form-input v-model="fileRename" placeholder="Имя"/>
            </div>
            <b-button class="mt-2" variant="outline-warning" block @click="fileRenamerOk">Применить</b-button>
        </b-modal>

        <b-card no-body>
            <template v-slot:header>
                <div style="display: flex; justify-content: space-between">
                    <h4 class="mb-0">Файлы <b-badge pill>{{files.length}}</b-badge></h4>
                    <div>
                        <b-button size="sm" style="color: #fff" variant="danger" :disabled="!$root.locked"
                                  @click="showRemoverCreator">Создать удалитель</b-button>
                        <b-button size="sm" style="color: #fff" variant="secondary" :disabled="!$root.locked"
                                  @click="showFolderCreator">Создать папку</b-button>
                        <b-button size="sm" style="color: #fff" variant="primary" :disabled="!$root.locked"
                                  @click="showFolderAdder">Добавить файлы</b-button>
                    </div>
                </div>
            </template>

            <div style="height: 418px; overflow: auto">
                <b-list-group flush>
                    <b-list-group-item v-for="f in folders" :key="f.name" button
                                       @click="switchFolder(f.name)"
                                       @contextmenu.prevent="ctxFolderOpen($event, f)"
                                       class="d-flex justify-content-between align-items-center">
                        <b-icon-folder style="margin-right: 5px"/> {{f.name}}
                        <b-badge style="margin-left: auto; margin-right: 10px;" pill>Файлы: {{f.files}}</b-badge>
                        <!--<b-badge style="margin-right: 10px;" pill>{{$root.formatBytes(f.size)}}</b-badge>-->
                        <b-badge pill>{{f.time}}</b-badge>

                        <b-dropdown id="folder-dd" size="sm" class="m-2" disabled
                                    v-b-tooltip.hover title="Используйте контекстное меню">
                            <b-dropdown-item @click="renameFolder(f)">Переименовать</b-dropdown-item>
                            <b-dropdown-item @click="removeFolder(f)">Удалить</b-dropdown-item>
                        </b-dropdown>
                    </b-list-group-item>

                    <b-list-group-item v-for="f in files" :key="f.name"
                                       @contextmenu.prevent="ctxOpen($event, f)"
                            class="d-flex justify-content-between align-items-center">
                        <b-icon-file-earmark style="margin-right: 5px"/> {{f.name}}
                        <b-badge style="margin-left: auto; margin-right: 10px;" pill>{{$root.formatBytes(f.size)}}</b-badge>
                        <b-badge pill>{{f.time}}</b-badge>

                        <b-dropdown id="file-dd" size="sm" class="m-2" :disabled="!$root.locked">
                            <b-dropdown-item @click="renameFile(f)">Переименовать</b-dropdown-item>
                            <b-dropdown-item @click="removeFile(f)">Удалить</b-dropdown-item>
                            <b-dropdown-item :href="'/download?file=' + getFilePath(f.name)" target="_blank">Скачать</b-dropdown-item>
                        </b-dropdown>
                    </b-list-group-item>

                    <b-list-group-item v-for="f in removers" :key="f"
                                       @contextmenu.prevent="ctxRemoverOpen($event, f)"
                                       class="d-flex justify-content-between align-items-center">
                        <b-icon-trash style="margin-right: 5px"/> {{f}}

                        <b-dropdown id="remover-dd" style="margin-left: auto !important;" size="sm" class="m-2" :disabled="!$root.locked">
                            <b-dropdown-item @click="removeRemover(f)">Удалить</b-dropdown-item>
                        </b-dropdown>
                    </b-list-group-item>
                </b-list-group>
            </div>
        </b-card>

        <vue-context ref="filemenu">
            <template slot-scope="child">
                <li>
                    <a href="#" @click.prevent="renameFile(child.data)">Переименовать</a>
                </li>
                <li>
                    <a href="#" @click.prevent="removeFile(child.data)">Удалить</a>
                </li>
                <li>
                    <a :href="fileUrl" target="_blank">Скачать</a>
                </li>
            </template>
        </vue-context>

        <vue-context ref="foldermenu">
            <template slot-scope="child">
                <li>
                    <a href="#" @click.prevent="renameFolder(child.data)">Переименовать</a>
                </li>
                <li>
                    <a href="#" @click.prevent="removeFolder(child.data)">Удалить</a>
                </li>
            </template>
        </vue-context>

        <vue-context ref="removermenu">
            <template slot-scope="child">
                <li>
                    <a href="#" @click.prevent="removeRemover(child.data)">Удалить</a>
                </li>
            </template>
        </vue-context>
    </div>
</template>

<script>
    import VueContext from 'vue-context';
    import vue2Dropzone from 'vue2-dropzone';
    import 'vue2-dropzone/dist/vue2Dropzone.min.css';

    // Import FilePond
    import vueFilePond from 'vue-filepond';

    // Import plugins
    import FilePondPluginImagePreview from 'filepond-plugin-image-preview/dist/filepond-plugin-image-preview.esm.js';

    // Import styles
    import 'filepond/dist/filepond.min.css';
    import 'filepond-plugin-image-preview/dist/filepond-plugin-image-preview.min.css';

    // Create FilePond component
    const FilePond = vueFilePond( FilePondPluginImagePreview );

    export default {
        name: "FilesBrowser",
        components: {
            VueContext,
            FilePond
        },
        data() {
            return {
                removerName: '',
                folderCreate: '',
                folderRename: '',
                folderRenameOri: '',

                fileRename: '',
                fileRenameOri: '',

                breadcrumbs: [],
                folders: [],
                files: [],
                removers: [],

                dropzoneOptions: {
                    url: '/upload',
                    thumbnailWidth: 150,
                    thumbnailHeight: 150,
                    addRemoveLinks: true,
                    chunking: true,
                    chunkSize: 20 * 1024 * 1024,
                    maxFilesize: null
                },
                labelIdle: 'Перетащите файлы для загрузки... или <span class="filepond--label-action">обзор</span>',
                server: {
                    process: {
                        url: '/process',
                        method: 'POST',
                        ondata: (formData) => {
                            formData.append('dir', this.$root.dir);
                            formData.append('socketId', this.$root.socketId);
                            return formData;
                        }
                    },
                    patch: '/upload?patch=',
                    revert: '/delete'
                },

                fileUrl: '#'
            };
        },
        sockets: {
            FolderBrowser(data) {
                this.$root.dir = data.folder;
                this.breadcrumbs = data.breadcrumbs;
                this.folders = data.folders;
                this.files = data.files;
                this.removers = data.removers;
            }
        },
        methods: {
            ctxFolderOpen(evt, f) {
                if (!this.$root.locked) {
                    return;
                }
                this.$refs.foldermenu.open(evt, f);
            },
            ctxOpen(evt, f) {
                if (!this.$root.locked) {
                    return;
                }
                this.fileUrl = '/download?file=' + this.getFilePath(f.name);
                this.$refs.filemenu.open(evt, f);
            },
            ctxRemoverOpen(evt, f) {
                if (!this.$root.locked) {
                    return;
                }
                this.$refs.removermenu.open(evt, f);
            },

            showFolderAdder() {
                this.$refs['folder-adder'].show();
            },

            sendingEvent (file, xhr, formData) {
                formData.append('dir', this.$root.dir);
                formData.append('socketId', this.$root.socketId);
            },

            uploadSuccess(file, response) {
                this.$refs['dropzone'].removeFile(file);
            },

            getFilePath(fname) {
                let file = '';
                if (this.$root.dir.length === 0) {
                    file = fname;
                } else {
                    file = this.$root.dir + '/' + fname;
                }
                return file;
            },

            showFolderCreator() {
                this.$refs['folder-creator'].show();
            },
            createFolder() {
                this.$refs['folder-creator'].hide();

                let dir = '';
                if (this.$root.dir.length === 0) {
                    dir = this.folderCreate;
                } else {
                    dir = this.$root.dir + '/' + this.folderCreate;
                }
                this.$root.$socket.invoke('CreateFolder', dir, this.$root.dir);

                this.folderCreate = '';
            },

            showRemoverCreator() {
                this.$refs['remover-creator'].show();
            },
            createRemover() {
                this.$refs['remover-creator'].hide();
                let file = '';
                if (this.$root.dir.length === 0) {
                    file = this.removerName;
                } else {
                    file = this.$root.dir + '/' + this.removerName;
                }
                this.$root.$socket.invoke('CreateRemover', file, this.$root.dir);
                this.removerName = '';
            },

            showFolderRenamer(folder) {
                this.folderRenameOri = folder;
                this.folderRename = folder;
                this.$refs['folder-renamer'].show();
            },
            folderRenamerOk() {
                this.$refs['folder-renamer'].hide();

                let dir = '';
                if (this.$root.dir.length === 0) {
                    dir = this.folderRenameOri;
                } else {
                    dir = this.$root.dir + '/' + this.folderRenameOri;
                }
                this.$root.$socket.invoke('RenameFolder', dir, this.folderRename, this.$root.dir);

                this.folderRename = '';
            },

            showFileRenamer(file) {
                this.fileRenameOri = file;
                this.fileRename = file;
                this.$refs['file-renamer'].show();
            },
            fileRenamerOk() {
                this.$refs['file-renamer'].hide();

                let file = '';
                if (this.$root.dir.length === 0) {
                    file = this.fileRenameOri;
                } else {
                    file = this.$root.dir + '/' + this.fileRenameOri;
                }
                this.$root.$socket.invoke('RenameFile', file, this.fileRename, this.$root.dir);

                this.fileRename = '';
            },

            rootClick() {
                this.$root.dir = 0;
                this.$root.$socket.invoke('SwitchFolder', '');
            },

            breadClick(dir) {
                this.$root.$socket.invoke('SwitchFolder', dir);
            },

            switchFolder(folder) {
                let dir = '';
                if (this.$root.dir.length === 0) {
                    dir = folder;
                } else {
                    dir = this.$root.dir + '/' + folder;
                }
                this.$root.$socket.invoke('SwitchFolder', dir);
            },

            removeFile(file) {
                let f = '';
                if (this.$root.dir.length === 0) {
                    f = file.name;
                } else {
                    f = this.$root.dir + '/' + file.name;
                }
                this.$root.$socket.invoke('RemoveFile', f, this.$root.dir);
            },
            renameFile(file) {
                this.showFileRenamer(file.name);
            },

            removeFolder(folder) {
                let dir = '';
                if (this.$root.dir.length === 0) {
                    dir = folder.name;
                } else {
                    dir = this.$root.dir + '/' + folder.name;
                }
                this.$root.$socket.invoke('RemoveFolder', dir, this.$root.dir);
            },
            renameFolder(folder) {
                this.showFolderRenamer(folder.name);
            },

            removeRemover(r) {
                let dir = '';
                if (this.$root.dir.length === 0) {
                    dir = r;
                } else {
                    dir = this.$root.dir + '/' + r;
                }
                this.$root.$socket.invoke('RemoveRemover', dir, this.$root.dir);
            },
        },
        mounted() {
           /* this.folders.push({
                name: 'test',
                files: 10,
                size: 1923456,
                time: '24.05.19'
            });

            for(let i = 0; i < 40; i++) {
                this.files.push({
                    name: 'AAA.txt',
                    size: 1923456,
                    time: '24.05.19'
                });
            }*/
        }
    }
</script>

<style lang="scss">
    #file-dd, #folder-dd, #remover-dd {
        height: 18px;
        > button {
            border-radius: 15px !important;
            &:empty::after {
                margin-bottom: 5px !important;
            }
        }
    }

    .vue-dropzone:hover {
        background-color: #555;
    }

    .dropzone {
        background: #222;
        border: none;
        min-height: 50px;
    }

    .dropzone-custom-title {
        margin-top: 0;
        color: #00b782;
    }

    .subtitle {
        color: #aaa;
    }

    #total-progress {
        position: relative;
        width: 100%;
        height: 30px;
        background-color: rgba(0,0,0,0.5);
        border: 1px solid #222;
    }
    #total-bar {
        position: absolute;
        top: 0;
        left: 0;
        height: 100%;
        background-color: #00b782;
        transition: width 0.5s;
    }

    .uploader {
        margin-bottom: 0;
    }
</style>