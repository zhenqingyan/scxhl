const { createApp, ref, reactive } = Vue;
const { MessagePlugin } = TDesign;

const app = createApp({
    setup() {
        // ─── 状态 ───────────────────────────────────────────────
        const loading = ref(false);
        const cleaning = ref(false);
        const duplicatePreviewLoading = ref(false);
        const duplicateDialogVisible = ref(false);
        const duplicateGroups = ref([]);
        const duplicateDeleteCount = ref(0);
        const uploadResult = reactive({
            visible: false,
            theme: 'success',
            message: ''
        });
        const tableData = ref([]);
        const pageData = reactive({
            current: 1,
            total: 0,
            pageSize: 12
        });
        const sortState = reactive({
            sortField: '',
            sortOrder: ''
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
                    pageSize: pageData.pageSize,
                    sortField: sortState.sortField,
                    sortOrder: sortState.sortOrder
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

        function setCreateTimeSort(order) {
            sortState.sortField = 'createTime';
            sortState.sortOrder = order === 'asc' ? 'asc' : 'desc';
            pageData.current = 1;
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

        // ─── 清理重复数据 ───────────────────────────────────────
        async function openDuplicateDialog() {
            if (duplicatePreviewLoading.value || cleaning.value) return;

            duplicateDialogVisible.value = true;
            duplicatePreviewLoading.value = true;
            duplicateGroups.value = [];
            duplicateDeleteCount.value = 0;

            try {
                var resp = await axios.post('/Product/GetDuplicateData');
                if (resp.data && resp.data.success) {
                    duplicateGroups.value = resp.data.groups || [];
                    duplicateDeleteCount.value = resp.data.deleteCount || 0;
                    if (duplicateDeleteCount.value === 0) {
                        MessagePlugin.info('未发现重复数据');
                    }
                } else {
                    duplicateDialogVisible.value = false;
                    MessagePlugin.error((resp.data && resp.data.message) || '检查重复数据失败');
                }
            } catch (e) {
                duplicateDialogVisible.value = false;
                MessagePlugin.error('检查重复数据请求失败');
                console.error(e);
            } finally {
                duplicatePreviewLoading.value = false;
            }
        }

        async function cleanDuplicateData() {
            if (cleaning.value || duplicateDeleteCount.value === 0) return;

            cleaning.value = true;
            try {
                var resp = await axios.post('/Product/CleanDuplicateData');
                if (resp.data && resp.data.success) {
                    MessagePlugin.success(resp.data.message || '清理完成');
                    duplicateDialogVisible.value = false;
                    duplicateGroups.value = [];
                    duplicateDeleteCount.value = 0;
                    pageData.current = 1;
                    await queryData();
                } else {
                    MessagePlugin.error((resp.data && resp.data.message) || '清理失败');
                }
            } catch (e) {
                MessagePlugin.error('清理请求失败');
                console.error(e);
            } finally {
                cleaning.value = false;
            }
        }

        // ─── 上传回调 ────────────────────────────────────────────
        function onUploadSuccess(ctx) {
            var summary = normalizeUploadResponse(ctx && ctx.response);
            if (summary.message) {
                uploadResult.visible = true;
                uploadResult.theme = summary.theme;
                uploadResult.message = summary.message;
                showUploadMessage(summary);
            } else {
                var fallbackMessage = (ctx.file && ctx.file.name || '文件') + ' 上传成功';
                uploadResult.visible = true;
                uploadResult.theme = 'success';
                uploadResult.message = fallbackMessage;
                MessagePlugin.success(fallbackMessage);
            }
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

        function imageUrl(guid) {
            return 'https://henglong.oss-cn-shanghai.aliyuncs.com/' + guid;
        }

        function formatHash(hash) {
            if (!hash) return '未生成';
            if (hash.length <= 18) return hash;
            return hash.slice(0, 10) + '...' + hash.slice(-6);
        }

        function normalizeUploadResponse(response) {
            var list = Array.isArray(response) ? response : [response];
            var total = {
                uploaded: 0,
                duplicate: 0,
                invalidType: 0,
                oversized: 0,
                identifyFailed: 0
            };

            list.forEach(function (item) {
                var data = parseUploadResponseItem(item);
                if (!data) return;
                total.uploaded += Number(data.uploaded) || 0;
                total.duplicate += Number(data.duplicate) || 0;
                total.invalidType += Number(data.invalidType) || 0;
                total.oversized += Number(data.oversized) || 0;
                total.identifyFailed += Number(data.identifyFailed) || 0;
            });

            var parts = ['成功上传 ' + total.uploaded + ' 个文件'];
            if (total.duplicate > 0) parts.push('跳过 ' + total.duplicate + ' 个重复文件');
            if (total.invalidType > 0) parts.push('跳过 ' + total.invalidType + ' 个格式不支持文件');
            if (total.oversized > 0) parts.push('跳过 ' + total.oversized + ' 个超大文件');
            if (total.identifyFailed > 0) parts.push('跳过 ' + total.identifyFailed + ' 个无法识别图片');

            var skipped = total.duplicate + total.invalidType + total.oversized + total.identifyFailed;
            if (total.uploaded === 0 && skipped === 0) {
                return { message: '', theme: 'success' };
            }

            return {
                message: parts.join('，'),
                theme: total.uploaded > 0 ? 'success' : (total.duplicate > 0 ? 'warning' : 'info')
            };
        }

        function parseUploadResponseItem(item) {
            if (!item) return null;
            var data = item;
            if (typeof data === 'string') {
                try {
                    data = JSON.parse(data);
                } catch (e) {
                    return null;
                }
            }
            if (data.response) return parseUploadResponseItem(data.response);
            if (data.data) return parseUploadResponseItem(data.data);
            return data;
        }

        function showUploadMessage(summary) {
            if (summary.theme === 'success') {
                MessagePlugin.success(summary.message);
            } else if (summary.theme === 'warning') {
                MessagePlugin.warning(summary.message);
            } else {
                MessagePlugin.info(summary.message);
            }
        }

        // ─── 初始化 ──────────────────────────────────────────────
        queryData();

        return {
            loading,
            cleaning,
            duplicatePreviewLoading,
            duplicateDialogVisible,
            duplicateGroups,
            duplicateDeleteCount,
            uploadResult,
            tableData,
            pageData,
            sortState,
            uploadUrl,
            editDialogVisible,
            editSaving,
            editFields,
            editForm,
            openEditDialog,
            saveEdit,
            queryData,
            onPageChange,
            setCreateTimeSort,
            delData,
            changeStatus,
            openDuplicateDialog,
            cleanDuplicateData,
            onUploadSuccess,
            onUploadFail,
            formatTime,
            imageUrl,
            formatHash
        };
    }
});

// 注册 TDesign
app.use(TDesign);
app.mount('#app');
