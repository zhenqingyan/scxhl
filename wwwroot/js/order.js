const { createApp, ref, reactive } = Vue;
const { MessagePlugin } = TDesign;

const orderApp = createApp({
    setup() {
        const loading = ref(false);
        const tableData = ref([]);
        const expandedId = ref('');
        const statusFilter = ref('all');
        const pageData = reactive({
            current: 1,
            total: 0,
            pageSize: 10
        });

        const statusOptions = [
            { value: 'all', label: '全部订单' },
            { value: 'pendingPay', label: '待付款' },
            { value: 'pendingShip', label: '待发货' },
            { value: 'shipped', label: '已发货' },
            { value: 'completed', label: '已完成' },
            { value: 'cancelled', label: '已取消' }
        ];

        const editableStatuses = statusOptions.filter(function (item) {
            return item.value !== 'all';
        });

        async function queryData() {
            loading.value = true;
            try {
                var resp = await axios.post('/Order/GetOrders', {
                    current: pageData.current,
                    pageSize: pageData.pageSize,
                    status: statusFilter.value
                });
                tableData.value = resp.data.data || [];
                pageData.total = resp.data.total || 0;
            } catch (e) {
                MessagePlugin.error('获取订单失败，请检查网络连接');
                console.error(e);
            } finally {
                loading.value = false;
            }
        }

        function onStatusChange() {
            pageData.current = 1;
            expandedId.value = '';
            queryData();
        }

        function onPageChange(pageInfo) {
            pageData.current = pageInfo.current;
            pageData.pageSize = pageInfo.pageSize;
            expandedId.value = '';
            queryData();
        }

        async function updateStatus(item, status) {
            if (item.status === status) return;
            var previous = item.status;
            item.status = status;
            try {
                var resp = await axios.post('/Order/UpdateStatus', {
                    orderId: item.id,
                    status: status
                });
                if (resp.data === '成功') {
                    MessagePlugin.success('订单状态已更新');
                    await queryData();
                } else {
                    item.status = previous;
                    MessagePlugin.warning(resp.data || '更新失败');
                }
            } catch (e) {
                item.status = previous;
                MessagePlugin.error('更新订单状态失败');
                console.error(e);
            }
        }

        function toggleExpand(item) {
            expandedId.value = expandedId.value === item.id ? '' : item.id;
        }

        function statusLabel(status) {
            var found = statusOptions.find(function (item) { return item.value === status; });
            return found ? found.label : status;
        }

        function statusTheme(status) {
            if (status === 'pendingPay') return 'warning';
            if (status === 'pendingShip') return 'primary';
            if (status === 'shipped') return 'success';
            if (status === 'completed') return 'success';
            if (status === 'cancelled') return 'default';
            return 'default';
        }

        function formatAddress(address) {
            if (!address) return '-';
            return [address.region, address.detail].filter(Boolean).join(' ');
        }

        function formatMoney(value) {
            var number = Number(value || 0);
            return number.toFixed(2);
        }

        function formatTime(dateStr) {
            if (!dateStr) return '';
            var d = new Date(dateStr);
            if (isNaN(d.getTime())) return dateStr;
            var pad = function (n) { return String(n).padStart(2, '0'); };
            return d.getFullYear() + '-' + pad(d.getMonth() + 1) + '-' + pad(d.getDate()) + ' ' + pad(d.getHours()) + ':' + pad(d.getMinutes());
        }

        queryData();

        return {
            loading,
            tableData,
            expandedId,
            statusFilter,
            pageData,
            statusOptions,
            editableStatuses,
            queryData,
            onStatusChange,
            onPageChange,
            updateStatus,
            toggleExpand,
            statusLabel,
            statusTheme,
            formatAddress,
            formatMoney,
            formatTime
        };
    }
});

orderApp.use(TDesign);
orderApp.mount('#order-app');
