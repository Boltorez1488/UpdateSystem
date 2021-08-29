import Vue from 'vue';
import VueSignalR from '@latelier/vue-signalr';
import { BootstrapVue, BootstrapVueIcons } from 'bootstrap-vue';

import 'bootstrap-vue/dist/bootstrap-vue-icons.min.css';
import 'vue-context/dist/css/vue-context.css';
import 'bootstrap/dist/css/bootstrap.css';
import 'bootstrap-vue/dist/bootstrap-vue.css';
import '@forevolve/bootstrap-dark/dist/css/bootstrap-dark.min.css';

Vue.use(BootstrapVue);
Vue.use(BootstrapVueIcons);
Vue.use(VueSignalR, '/panel');

Vue.component('main-panel', require('./components/MainPanel').default);

window.app = new Vue({
    el: '#app',
    data() {
        return {
            dir: '',
            socketId: null,
            folderAdder: false,
            locked: false
        };
    },
    sockets: {
        ConnectionId(id) {
            this.socketId = id;
        },
        DownloadState(locked) {
            this.locked = locked;
        }
    },
    created() {
        this.$socket.start({
            log: false
        });
    },
    methods: {
        formatBytes(bytes, decimals = 2) {
            if (bytes === 0) return '0 Байт';

            const k = 1024;
            const dm = decimals < 0 ? 0 : decimals;
            const sizes = ['Б', 'КБ', 'МБ', 'ГБ', 'ТБ', 'ПБ', 'EB', 'ZB', 'YB'];

            const i = Math.floor(Math.log(bytes) / Math.log(k));

            return parseFloat((bytes / Math.pow(k, i)).toFixed(dm)) + ' ' + sizes[i];
        }
    },
    mounted() {
        this.$socket.invoke('Accept', this.dir);
    }
});