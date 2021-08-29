import Vue from 'vue';
import BootstrapVue from 'bootstrap-vue';

import 'bootstrap/dist/css/bootstrap.css';
import 'bootstrap-vue/dist/bootstrap-vue.css';
import '@forevolve/bootstrap-dark/dist/css/bootstrap-dark.min.css';

Vue.use(BootstrapVue);

window.app = new Vue({
    el: '#app'
});