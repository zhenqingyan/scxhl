const { createApp, ref, reactive } = Vue;
const { MessagePlugin } = TDesign;

const app = createApp({
    setup() {
        // ─── 状态 ───────────────────────────────────────────────
        const loading = ref(false);
        const tableData = ref([]);
        const pageData = reactive({
            current: 1,
            total: 0,
            pageSize: 12
        });

        // 上传接口地址
        const uploadUrl = ref('/Product/ExportFile');

        // ─── 编辑对话框 ──────────────────────────────────────────
        const editDialogVisible = ref(false);
        const editSaving = ref(false);
        const editItem = ref(null);
        const editFields = [
            { key: 'level', label: '排序', type: 'number' },
            { key: 'number', label: '编号', type: 'text' },
            { key: 'composition', label: '成份', type: 'text' },
            { key: 'yarnCount', label: '纱支', type: 'text' },
            { key: 'density', label: '密度', type: 'text' },
            { key: 'gramWeight', label: '克重', type: 'text' },
            { key: 'doorframe', label: '门幅', type: 'text' },
            { key: 'width', label: '宽', type: 'number' },
            { key: 'height', label: '高', type: 'number' },
            { key: 'percent', label: '比例', type: 'text', readonly: true }
        ];
        const editForm = reactive({});

        function openEditDialog(item) {
            editItem.value = item;
            editFields.forEach(function (f) {
                editForm[f.key] = item[f.key] != null ? item[f.key] : '';
            });
            editDialogVisible.value = true;
        }

        async function saveEdit() {
            if (!editItem.value) return;

            var data = { guid: editItem.value.guid };
            editFields.forEach(function (f) {
                if (f.type === 'number') {
                    data[f.key] = Number(editForm[f.key]) || 0;
                } else {
                    data[f.key] = editForm[f.key];
                }
            });

            editSaving.value = true;
            try {
                var resp = await axios.post('/Product/UpdateLevel', data);
                if (resp.data === '成功') {
                    MessagePlugin.success('保存成功');
                    editDialogVisible.value = false;
                    await queryData();
                } else {
                    MessagePlugin.warning('保存失败：' + resp.data);
                }
            } catch (e) {
                MessagePlugin.error('保存请求失败');
                console.error(e);
            } finally {
                editSaving.value = false;
            }
        }

        // ─── 数据查询 ────────────────────────────────────────────
        async function queryData() {
            loading.value = true;
            try {
                var resp = await axios.post('/Product/GetImgs', {
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

        // ─── 分页变化 ────────────────────────────────────────────
        function onPageChange(pageInfo) {
            pageData.current = pageInfo.current;
            pageData.pageSize = pageInfo.pageSize;
            queryData();
        }

        // ─── 删除 ────────────────────────────────────────────────
        async function delData(item) {
            try {
                var resp = await axios.post('/Product/Del', { guid: item.guid });
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

        // ─── 启用/禁用 ───────────────────────────────────────────
        async function changeStatus(status, item) {
            try {
                var resp = await axios.post('/Product/Update', {
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
            MessagePlugin.success((ctx.file && ctx.file.name || '文件') + ' 上传成功');
            queryData();
        }

        function onUploadFail(ctx) {
            MessagePlugin.error((ctx.file && ctx.file.name || '文件') + ' 上传失败');
        }

        // ─── 工具函数 ────────────────────────────────────────────
        function formatTime(dateStr) {
            if (!dateStr) return '';
            var d = new Date(dateStr);
            if (isNaN(d.getTime())) return dateStr;
            var pad = function (n) { return String(n).padStart(2, '0'); };
            return d.getFullYear() + '-' + pad(d.getMonth() + 1) + '-' + pad(d.getDate()) + ' ' + pad(d.getHours()) + ':' + pad(d.getMinutes());
        }

        // ─── 初始化 ──────────────────────────────────────────────
        queryData();

        return {
            loading,
            tableData,
            pageData,
            uploadUrl,
            editDialogVisible,
            editSaving,
            editFields,
            editForm,
            openEditDialog,
            saveEdit,
            queryData,
            onPageChange,
            delData,
            changeStatus,
            onUploadSuccess,
            onUploadFail,
            formatTime
        };
    }
});

// 注册 TDesign
app.use(TDesign);
app.mount('#app');
