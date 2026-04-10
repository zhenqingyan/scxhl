const { createApp, ref, reactive } = Vue;
const { MessagePlugin } = TDesignVueNext;

const app = createApp({
    setup() {
        // ─── 状态 ───────────────────────────────────────────────
        const loading = ref(false);
        const tableData = ref([]);
        const pageData = reactive({
            current: 1,
            total: 0,
            pageSize: 9
        });

        // 上传接口地址（相对路径）
        const uploadUrl = ref('/Product/ExportFile');

        // ─── 数据查询 ────────────────────────────────────────────
        async function queryData() {
            loading.value = true;
            try {
                const resp = await axios.post('/Product/GetImgs', {
                    current: pageData.current,
                    pageSize: pageData.pageSize
                });
                tableData.value = resp.data.data;
                pageData.total = resp.data.total;
            } catch (e) {
                MessagePlugin.error('获取数据失败，请检查网络连接');
                console.error(e);
            } finally {
                loading.value = false;
            }
        }

        // ─── 删除 ────────────────────────────────────────────────
        async function delData(item) {
            try {
                const resp = await axios.post('/Product/Del', { guid: item.guid });
                if (resp.data === 1) {
                    MessagePlugin.success('删除成功');
                    await queryData();
                } else {
                    MessagePlugin.error('删除失败');
                }
            } catch (e) {
                MessagePlugin.error('删除请求失败');
                console.error(e);
            }
        }

        // ─── 保存属性 ────────────────────────────────────────────
        async function saveItem(item) {
            try {
                const resp = await axios.post('/Product/UpdateLevel', {
                    guid: item.guid,
                    level: item.level,
                    number: item.number,
                    composition: item.composition,
                    yarnCount: item.yarnCount,
                    density: item.density,
                    gramWeight: item.gramWeight,
                    doorframe: item.doorframe,
                    width: item.width,
                    height: item.height,
                    percent: item.percent
                });
                if (resp.data === '成功') {
                    MessagePlugin.success('保存成功');
                    // 刷新 percent 字段（后端重新计算）
                    await queryData();
                } else {
                    MessagePlugin.warning('保存失败：' + resp.data);
                }
            } catch (e) {
                MessagePlugin.error('保存请求失败');
                console.error(e);
            }
        }

        // ─── 启用/禁用 ───────────────────────────────────────────
        async function changeStatus(status, item) {
            try {
                const resp = await axios.post('/Product/Update', {
                    guid: item.guid,
                    status: status
                });
                MessagePlugin.success(status ? '已启用' : '已禁用');
            } catch (e) {
                // 回滚状态
                item.status = !status;
                MessagePlugin.error('状态更新失败');
                console.error(e);
            }
        }

        // ─── 上传回调 ────────────────────────────────────────────
        function onUploadSuccess(ctx) {
            MessagePlugin.success(`${ctx.file.name} 上传成功`);
            queryData();
        }

        function onUploadFail(ctx) {
            MessagePlugin.error(`${ctx.file.name} 上传失败`);
        }

        // ─── 分页 ────────────────────────────────────────────────
        function onPageChange(pageInfo) {
            pageData.current = pageInfo.current;
            queryData();
        }

        function onPageSizeChange(pageSize) {
            pageData.pageSize = pageSize;
            pageData.current = 1;
            queryData();
        }

        // ─── 工具函数 ────────────────────────────────────────────
        function formatTime(dateStr) {
            if (!dateStr) return '';
            const d = new Date(dateStr);
            if (isNaN(d.getTime())) return dateStr;
            const pad = n => String(n).padStart(2, '0');
            return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())} ${pad(d.getHours())}:${pad(d.getMinutes())}`;
        }

        // ─── 初始化 ──────────────────────────────────────────────
        queryData();

        return {
            loading,
            tableData,
            pageData,
            uploadUrl,
            queryData,
            delData,
            saveItem,
            changeStatus,
            onUploadSuccess,
            onUploadFail,
            onPageChange,
            onPageSizeChange,
            formatTime
        };
    }
});

// 注册 TDesign
app.use(TDesignVueNext);
app.mount('#app');
