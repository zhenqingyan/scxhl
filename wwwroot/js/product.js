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
                renderPagination();
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

        // ─── 编辑对话框（纯DOM方式） ─────────────────────────────────
        const fields = [
            { key: 'level', label: '排序', type: 'number', min: 1 },
            { key: 'number', label: '编号', type: 'text' },
            { key: 'composition', label: '成份', type: 'text' },
            { key: 'yarnCount', label: '纱支', type: 'text' },
            { key: 'density', label: '密度', type: 'text' },
            { key: 'gramWeight', label: '克重', type: 'text' },
            { key: 'doorframe', label: '门幅', type: 'text' },
            { key: 'width', label: '宽', type: 'number', min: 0 },
            { key: 'height', label: '高', type: 'number', min: 0 },
            { key: 'percent', label: '比例', type: 'text', readonly: true }
        ];

        function openEditDialog(item) {
            // 移除已有的对话框
            const existing = document.getElementById('edit-dialog-overlay');
            if (existing) existing.remove();

            // 创建遮罩层
            const overlay = document.createElement('div');
            overlay.id = 'edit-dialog-overlay';
            overlay.style.cssText = 'position:fixed;top:0;left:0;right:0;bottom:0;background:rgba(0,0,0,0.5);display:flex;align-items:center;justify-content:center;z-index:9999;';

            // 创建对话框
            const dialog = document.createElement('div');
            dialog.style.cssText = 'background:#fff;border-radius:8px;width:600px;max-width:90%;max-height:90vh;overflow:hidden;box-shadow:0 4px 20px rgba(0,0,0,0.15);';

            // 标题栏
            const header = document.createElement('div');
            header.style.cssText = 'display:flex;justify-content:space-between;align-items:center;padding:16px 20px;border-bottom:1px solid #e7e7e7;background:#f5f6fa;';
            header.innerHTML = '<h3 style="margin:0;font-size:16px;font-weight:600;color:#333;">编辑产品属性</h3>';
            const closeBtn = document.createElement('button');
            closeBtn.textContent = '\u00D7';
            closeBtn.style.cssText = 'background:none;border:none;font-size:24px;color:#999;cursor:pointer;padding:0;line-height:1;';
            closeBtn.onclick = function () { overlay.remove(); };
            header.appendChild(closeBtn);
            dialog.appendChild(header);

            // 表单区域
            const body = document.createElement('div');
            body.style.cssText = 'padding:20px;max-height:60vh;overflow-y:auto;';

            const grid = document.createElement('div');
            grid.style.cssText = 'display:grid;grid-template-columns:repeat(3,1fr);gap:16px;';

            const inputs = {};
            fields.forEach(function (f) {
                const cell = document.createElement('div');

                const label = document.createElement('label');
                label.textContent = f.label;
                label.style.cssText = 'display:block;font-size:12px;color:#666;margin-bottom:4px;';
                cell.appendChild(label);

                const input = document.createElement('input');
                input.type = f.type;
                input.value = item[f.key] != null ? item[f.key] : '';
                input.style.cssText = 'width:100%;padding:6px 10px;border:1px solid #d9d9d9;border-radius:4px;font-size:14px;outline:none;box-sizing:border-box;';
                if (f.min !== undefined) input.min = f.min;
                if (f.readonly) {
                    input.readOnly = true;
                    input.style.background = '#f5f6fa';
                }
                input.onfocus = function () { input.style.borderColor = '#0052d9'; };
                input.onblur = function () { input.style.borderColor = '#d9d9d9'; };

                inputs[f.key] = input;
                cell.appendChild(input);
                grid.appendChild(cell);
            });

            body.appendChild(grid);
            dialog.appendChild(body);

            // 底部按钮
            const footer = document.createElement('div');
            footer.style.cssText = 'display:flex;justify-content:flex-end;gap:12px;padding:16px 20px;border-top:1px solid #e7e7e7;background:#fafafa;';

            const cancelBtn = document.createElement('button');
            cancelBtn.textContent = '取消';
            cancelBtn.style.cssText = 'padding:8px 20px;border:1px solid #d9d9d9;border-radius:4px;background:#fff;cursor:pointer;font-size:14px;color:#333;';
            cancelBtn.onmouseenter = function () { cancelBtn.style.borderColor = '#0052d9'; cancelBtn.style.color = '#0052d9'; };
            cancelBtn.onmouseleave = function () { cancelBtn.style.borderColor = '#d9d9d9'; cancelBtn.style.color = '#333'; };
            cancelBtn.onclick = function () { overlay.remove(); };

            const confirmBtn = document.createElement('button');
            confirmBtn.textContent = '确认';
            confirmBtn.style.cssText = 'padding:8px 20px;border:none;border-radius:4px;background:#0052d9;color:#fff;cursor:pointer;font-size:14px;';
            confirmBtn.onmouseenter = function () { confirmBtn.style.background = '#003cab'; };
            confirmBtn.onmouseleave = function () { confirmBtn.style.background = '#0052d9'; };
            confirmBtn.onclick = async function () {
                var data = { guid: item.guid };
                fields.forEach(function (f) {
                    if (f.type === 'number') {
                        data[f.key] = Number(inputs[f.key].value) || 0;
                    } else {
                        data[f.key] = inputs[f.key].value;
                    }
                });

                try {
                    confirmBtn.disabled = true;
                    confirmBtn.textContent = '保存中...';
                    var resp = await axios.post('/Product/UpdateLevel', data);
                    if (resp.data === '成功') {
                        MessagePlugin.success('保存成功');
                        overlay.remove();
                        await queryData();
                    } else {
                        MessagePlugin.warning('保存失败：' + resp.data);
                        confirmBtn.disabled = false;
                        confirmBtn.textContent = '确认';
                    }
                } catch (e) {
                    MessagePlugin.error('保存请求失败');
                    console.error(e);
                    confirmBtn.disabled = false;
                    confirmBtn.textContent = '确认';
                }
            };

            footer.appendChild(cancelBtn);
            footer.appendChild(confirmBtn);
            dialog.appendChild(footer);

            overlay.appendChild(dialog);
            document.body.appendChild(overlay);
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

        // ─── 分页（纯DOM方式） ──────────────────────────────────
        const pageSizeOptions = [9, 18, 27];

        function renderPagination() {
            const container = document.getElementById('pagination-container');
            if (!container) return;
            container.innerHTML = '';

            var total = pageData.total;
            var current = pageData.current;
            var ps = pageData.pageSize;
            var totalPages = Math.ceil(total / ps);

            if (total <= 0) return;

            // 外层容器
            var wrap = document.createElement('div');
            wrap.style.cssText = 'display:flex;align-items:center;justify-content:center;gap:8px;margin-top:24px;padding:16px 0;background:#fff;border-radius:8px;flex-wrap:wrap;';

            // 总条数
            var info = document.createElement('span');
            info.textContent = '共 ' + total + ' 条';
            info.style.cssText = 'font-size:13px;color:#666;margin-right:8px;';
            wrap.appendChild(info);

            // 每页条数选择
            var sizeSelect = document.createElement('select');
            sizeSelect.style.cssText = 'padding:4px 8px;border:1px solid #d9d9d9;border-radius:4px;font-size:13px;color:#333;cursor:pointer;outline:none;';
            pageSizeOptions.forEach(function (opt) {
                var option = document.createElement('option');
                option.value = opt;
                option.textContent = opt + ' 条/页';
                if (opt === ps) option.selected = true;
                sizeSelect.appendChild(option);
            });
            sizeSelect.onchange = function () {
                pageData.pageSize = Number(sizeSelect.value);
                pageData.current = 1;
                queryData();
            };
            wrap.appendChild(sizeSelect);

            // 分隔
            var sep = document.createElement('span');
            sep.style.cssText = 'width:12px;';
            wrap.appendChild(sep);

            // 创建页码按钮的辅助函数
            function createBtn(text, page, isActive, isDisabled) {
                var btn = document.createElement('button');
                btn.textContent = text;
                btn.disabled = isDisabled;
                var base = 'min-width:32px;height:32px;padding:0 8px;border-radius:4px;font-size:13px;cursor:pointer;border:1px solid ';
                if (isActive) {
                    btn.style.cssText = base + '#0052d9;background:#0052d9;color:#fff;';
                } else if (isDisabled) {
                    btn.style.cssText = base + '#d9d9d9;background:#f5f5f5;color:#bbb;cursor:not-allowed;';
                } else {
                    btn.style.cssText = base + '#d9d9d9;background:#fff;color:#333;';
                    btn.onmouseenter = function () { btn.style.borderColor = '#0052d9'; btn.style.color = '#0052d9'; };
                    btn.onmouseleave = function () { btn.style.borderColor = '#d9d9d9'; btn.style.color = '#333'; };
                }
                if (!isDisabled && !isActive) {
                    btn.onclick = function () {
                        pageData.current = page;
                        queryData();
                    };
                }
                return btn;
            }

            // 上一页
            wrap.appendChild(createBtn('<', current - 1, false, current <= 1));

            // 页码按钮
            var pages = [];
            if (totalPages <= 7) {
                for (var i = 1; i <= totalPages; i++) pages.push(i);
            } else {
                pages.push(1);
                if (current > 4) pages.push('...');
                var start = Math.max(2, current - 2);
                var end = Math.min(totalPages - 1, current + 2);
                for (var i = start; i <= end; i++) pages.push(i);
                if (current < totalPages - 3) pages.push('...');
                pages.push(totalPages);
            }

            pages.forEach(function (p) {
                if (p === '...') {
                    var dots = document.createElement('span');
                    dots.textContent = '...';
                    dots.style.cssText = 'padding:0 4px;color:#999;font-size:13px;';
                    wrap.appendChild(dots);
                } else {
                    wrap.appendChild(createBtn(String(p), p, p === current, false));
                }
            });

            // 下一页
            wrap.appendChild(createBtn('>', current + 1, false, current >= totalPages));

            // 跳转输入
            var sep2 = document.createElement('span');
            sep2.style.cssText = 'width:8px;';
            wrap.appendChild(sep2);

            var jumpLabel = document.createElement('span');
            jumpLabel.textContent = '跳至';
            jumpLabel.style.cssText = 'font-size:13px;color:#666;';
            wrap.appendChild(jumpLabel);

            var jumpInput = document.createElement('input');
            jumpInput.type = 'number';
            jumpInput.min = 1;
            jumpInput.max = totalPages;
            jumpInput.style.cssText = 'width:50px;padding:4px 6px;border:1px solid #d9d9d9;border-radius:4px;font-size:13px;text-align:center;outline:none;';
            jumpInput.onkeydown = function (e) {
                if (e.key === 'Enter') {
                    var val = parseInt(jumpInput.value);
                    if (val >= 1 && val <= totalPages) {
                        pageData.current = val;
                        queryData();
                    }
                }
            };
            wrap.appendChild(jumpInput);

            var jumpLabel2 = document.createElement('span');
            jumpLabel2.textContent = '页';
            jumpLabel2.style.cssText = 'font-size:13px;color:#666;';
            wrap.appendChild(jumpLabel2);

            container.appendChild(wrap);
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
            changeStatus,
            onUploadSuccess,
            onUploadFail,
            formatTime,
            openEditDialog
        };
    }
});

// 注册 TDesign
app.use(TDesign);
app.mount('#app');
