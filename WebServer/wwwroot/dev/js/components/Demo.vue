<template>
    <div>
        <div>{{msg}}</div>
        <div>Online: {{online}}</div>
    </div>
</template>

<script>
    export default {
        name: "Demo",
        data() {
            return {
                msg: "Nope",
                online: 0
            };
        },
        mounted() {
            this.$root.$socket.invoke('SendMessage', 'hi', 'ne hi');
        },
        sockets: {
            ReceiveMessage(data) {
                this.msg = data.a1;
            },
            OnlineChanged(count) {
                this.online = count;
            }
        }
    }
</script>

<style scoped>

</style>