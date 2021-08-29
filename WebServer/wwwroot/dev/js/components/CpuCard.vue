<template>
    <b-card no-body>
        <template v-slot:header>
            <h4 class="mb-0">Сервер</h4>
        </template>

        <b-list-group flush>
            <b-list-group-item>Ядер: {{cores}}</b-list-group-item>
            <b-list-group-item>Память процесса: {{$root.formatBytes(memory)}}</b-list-group-item>
            <b-list-group-item>Загрузка CPU: {{usageCPU}}%</b-list-group-item>

            <b-list-group-item>Память ОС: {{totalMemory}}</b-list-group-item>
            <b-list-group-item>Используемая память: {{usedMemory}}</b-list-group-item>
            <b-list-group-item>Свободная память: {{freeMemory}}</b-list-group-item>
        </b-list-group>
    </b-card>
</template>

<script>
    export default {
        name: "CpuCard",
        data() {
            return {
                cores: 0,
                memory: 0,
                usageCPU: 0,

                totalMemory: '0',
                usedMemory: '0',
                freeMemory: '0'
            };
        },
        sockets: {
            CPU(cpu) {
                this.cores = cpu.cores;
                this.memory = cpu.processMemory;
                this.usageCPU = cpu.usageCPU.toFixed(2);

                this.totalMemory = cpu.totalMemory + ' МБ';
                this.usedMemory = cpu.usedMemory + ' МБ';
                this.freeMemory = cpu.freeMemory + ' МБ';
            }
        }
    }
</script>

<style scoped>

</style>