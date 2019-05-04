$(function () {

    let vueInstance = function () {
        var init = function () {
            var vue = new Vue({
                el: "#app",
                data: function () {
                    let _self = this;
                    return {
                        tableData: {
                            data: [],
                            loading: false
                        },
                        pageData: {
                            current: 1,
                            total: 100,
                            pageSize: 9
                        }
                    };
                },
                methods: {
                    QueryData: function () {
                        let _self = this;
                        console.log("Querydata");
                        _self.$Spin.show();
                        _self.tableData.loading = true;
                        axios.post('GetImgs', {
                            current: _self.pageData.current,
                            pageSize: _self.pageData.pageSize
                        }).then(function (resp) {
                            _self.tableData.data = resp.data.data;
                            _self.pageData.total = resp.data.total;
                            _self.tableData.loading = false;
                            _self.$Spin.hide();
                        })
                    },
                    delData: function (item) {
                        let _self = this;
                        _self.$Spin.show();
                        axios.post("Del", {
                            guid: item.guid
                        }).then(function (resp) {
                            _self.$Spin.hide();
                            _self.$Message.info(resp.data == 1 ? "success del one" : "failed del one");
                            _self.QueryData();
                        })
                        console.log(item.guid)
                    },
                    saveLevel: function (item) {
                        let _self = this;
                        _self.$Spin.show();
                        axios.post('UpdateLevel', {
                            guid: item.guid,
                            level: item.level
                        }).then(function (resp) {
                            _self.$Spin.hide();
                            _self.$Message.info(resp.data);
                        });
                    },
                    changeStatus: function (status, item) {
                        let _self = this;
                        axios.post('Update', {
                            guid: item.guid,
                            status: status
                        }).then(function (resp) {
                            _self.$Message.info(resp.data);
                        });
                    },
                    changePage: function () {
                        this.QueryData();
                    }
                },
                mounted: function () {
                    let _self = this;
                    _self.QueryData();
                }
            });
            return {
                vm: vue
            }
        }
        return {
            Init: init
        }
    }();

    vm = vueInstance.Init();
})