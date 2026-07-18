(function () {
    const el = document.getElementById('mini-admin-app');
    if (!el || !window.Vue || !window.TDesign || !window.axios) return;

    const { createApp, ref, reactive, computed } = Vue;
    const { MessagePlugin } = TDesign;

    const endpoints = {
        users: '/MiniAdmin/GetUsers',
        cart: '/MiniAdmin/GetCartItems',
        favorites: '/MiniAdmin/GetFavorites',
        addresses: '/MiniAdmin/GetAddresses'
    };

    const app = createApp({
        setup() {
            const activeTab = ref('users');
            const keyword = ref('');
            const loading = ref(false);
            const tableData = ref([]);
            const pageData = reactive({
                current: 1,
                total: 0,
                pageSize: 10
            });

            const formatTime = function (value) {
                if (!value) return '-';
                const date = new Date(value);
                if (Number.isNaN(date.getTime())) return value;
                const pad = function (number) { return String(number).padStart(2, '0'); };
                return date.getFullYear() + '-' + pad(date.getMonth() + 1) + '-' + pad(date.getDate()) + ' ' + pad(date.getHours()) + ':' + pad(date.getMinutes());
            };

            const productText = function (row) {
                return row.productNumber || row.productName || row.productGuid || '-';
            };

            const columns = computed(function () {
                if (activeTab.value === 'users') {
                    return [
                        { colKey: 'userId', title: '用户ID', ellipsis: true, width: 220 },
                        { colKey: 'openId', title: 'OpenId', ellipsis: true },
                        { colKey: 'cartCount', title: '购物车', width: 90 },
                        { colKey: 'favoriteCount', title: '收藏', width: 80 },
                        { colKey: 'addressCount', title: '地址', width: 80 },
                        { colKey: 'orderCount', title: '订单', width: 80 },
                        { colKey: 'createTime', title: '创建时间', width: 170, cell: (h, { row }) => formatTime(row.createTime) }
                    ];
                }

                if (activeTab.value === 'cart') {
                    return [
                        { colKey: 'userId', title: '用户ID', ellipsis: true, width: 220 },
                        { colKey: 'product', title: '商品', ellipsis: true, cell: (h, { row }) => productText(row) },
                        { colKey: 'composition', title: '成分', ellipsis: true, width: 140 },
                        { colKey: 'quantity', title: '数量', width: 80 },
                        { colKey: 'selected', title: '已选', width: 80, cell: (h, { row }) => row.selected ? '是' : '否' },
                        { colKey: 'updateTime', title: '更新时间', width: 170, cell: (h, { row }) => formatTime(row.updateTime) }
                    ];
                }

                if (activeTab.value === 'favorites') {
                    return [
                        { colKey: 'userId', title: '用户ID', ellipsis: true, width: 220 },
                        { colKey: 'product', title: '商品', ellipsis: true, cell: (h, { row }) => productText(row) },
                        { colKey: 'composition', title: '成分', ellipsis: true, width: 160 },
                        { colKey: 'createTime', title: '收藏时间', width: 170, cell: (h, { row }) => formatTime(row.createTime) }
                    ];
                }

                return [
                    { colKey: 'userId', title: '用户ID', ellipsis: true, width: 220 },
                    { colKey: 'name', title: '收货人', width: 110 },
                    { colKey: 'phone', title: '电话', width: 140 },
                    { colKey: 'region', title: '地区', ellipsis: true, width: 180 },
                    { colKey: 'detail', title: '详细地址', ellipsis: true },
                    { colKey: 'isDefault', title: '默认', width: 80, cell: (h, { row }) => row.isDefault ? '是' : '否' }
                ];
            });

            const withRowKey = function (items) {
                return items.map(function (item, index) {
                    return Object.assign({ rowKey: activeTab.value + '-' + pageData.current + '-' + index }, item);
                });
            };

            const queryData = async function () {
                loading.value = true;
                try {
                    const response = await axios.post(endpoints[activeTab.value], {
                        current: pageData.current,
                        pageSize: pageData.pageSize,
                        keyword: keyword.value
                    });
                    tableData.value = withRowKey(response.data.data || []);
                    pageData.total = response.data.total || 0;
                } catch (error) {
                    MessagePlugin.error('获取数据失败');
                    console.error(error);
                } finally {
                    loading.value = false;
                }
            };

            const search = function () {
                pageData.current = 1;
                queryData();
            };

            const onTabChange = function () {
                pageData.current = 1;
                pageData.total = 0;
                tableData.value = [];
                queryData();
            };

            const onPageChange = function (pageInfo) {
                pageData.current = pageInfo.current;
                pageData.pageSize = pageInfo.pageSize;
                queryData();
            };

            queryData();

            return {
                activeTab,
                keyword,
                loading,
                tableData,
                pageData,
                columns,
                queryData,
                search,
                onTabChange,
                onPageChange
            };
        }
    });

    app.use(TDesign);
    app.mount(el);
})();
