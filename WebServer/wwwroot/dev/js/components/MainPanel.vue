<template>
    <div v-on:dragover="dragDetect" v-on:drop="drop">
        <nav-bar />
        <div style="margin-top: 15px"/>
        <div style="display: flex; flex-wrap: wrap">
            <div class="col-md-4">
                <cpu-card />
            </div>
            <div class="col-md-8">
                <files-browser/>
            </div>
        </div>
    </div>
</template>

<script>
    import NavBar from "./NavBar";
    import CpuCard from "./CpuCard";
    import FilesBrowser from "./FilesBrowser";
    export default {
        name: "MainPanel",
        components: {FilesBrowser, CpuCard, NavBar},
        methods: {
            dragDetect(e) {
                if (!this.$root.locked) {
                    e.preventDefault();
                    e.stopPropagation();
                    e.dataTransfer.effectAllowed = "none";
                    e.dataTransfer.dropEffect = "none";
                    return;
                }
                this.$root.folderAdder = true;
            },
            drop(e) {
                if (!this.$root.locked) {
                    e.preventDefault();
                    e.stopPropagation();
                    e.dataTransfer.effectAllowed = "none";
                    e.dataTransfer.dropEffect = "none";
                }
            }
        }
    }
</script>

<style lang="scss">
    body:not(.bootstrap-dark) {
        background-color: #222;
    }

    ::-webkit-scrollbar {
        width: 20px;
    }
    ::-webkit-scrollbar-track {
        background: #3e3e42;
        //-webkit-box-shadow: inset 1px 1px 2px rgba(0,0,0,0.1);
    }
    ::-webkit-scrollbar-thumb {
        background: #686868;
        border: 4px solid #3e3e42;
        //-webkit-box-shadow: inset 1px 1px 2px rgba(0,0,0,0.2);
    }
    ::-webkit-scrollbar-thumb:hover {
        background: #9e9e9e;
    }
    ::-webkit-scrollbar-thumb:active {
        background: #efebef;
        //-webkit-box-shadow: inset 1px 1px 2px rgba(0,0,0,0.3);
    }
</style>