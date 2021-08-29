<template>
    <b-navbar toggleable="lg" type="dark" variant="dark">
        <b-navbar-brand href="">WebPanel</b-navbar-brand>
        <b-button squared variant="outline-danger" style="margin-right: 10px"
                  title="Заблокировать скачивание и разблокировать работу с файлами"
                  :disabled="$root.locked" @click="lockDownload">Блокировка</b-button>
        <b-button squared variant="outline-success"
                  title="Разблокировать скачивание и заблокировать работу с файлами"
                  :disabled="!$root.locked" @click="unlockDownload">Разблокировка</b-button>
        <b-collapse id="nav-collapse" is-nav>
            <b-navbar-nav class="ml-auto">
                <b-nav-text id="files-reload-text" v-if="isReload">Файлы обновляются</b-nav-text>
                <b-nav-text id="downloads-counter">Скачивают: {{downloads}}</b-nav-text>
                <b-nav-text style="color: greenyellow">Онлайн: {{online}}</b-nav-text>
            </b-navbar-nav>
        </b-collapse>
    </b-navbar>
</template>

<script>
    export default {
        name: "NavBar",
        data() {
            return {
                online: 0,
                downloads: 0,
                isReload: false
            };
        },
        sockets: {
            DownloadsChanged(count) {
                this.downloads = count;
            },
            OnlineChanged(count) {
                this.online = count;
            },
            FilesReload(isReload) {
                this.isReload = isReload;
            }
        },
        methods: {
            lockDownload() {
                this.$root.$socket.invoke('DownloadLock', true);
            },
            unlockDownload() {
                this.$root.$socket.invoke('DownloadLock', false);
            }
        }
    }
</script>

<style scoped lang="scss">
    #files-reload-text {
        color: orange;
        margin-right: 5px;
        border-right: 1px solid #555;
        padding-right: 5px;
    }

    #downloads-counter {
        color: yellow;
        margin-right: 10px;
    }
</style>