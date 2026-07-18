(function () {
    const el = document.getElementById('admin-users-app');
    if (!el || !window.Vue || !window.TDesign || !window.axios) return;

    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

    const app = Vue.createApp({
        setup() {
            const TButton = Vue.resolveComponent('t-button');
            const TSpace = Vue.resolveComponent('t-space');
            const users = Vue.ref([]);
            const loading = Vue.ref(false);
            const saving = Vue.ref(false);
            const createVisible = Vue.ref(false);
            const resetVisible = Vue.ref(false);
            const createForm = Vue.reactive({ username: '', password: '' });
            const resetForm = Vue.reactive({ username: '', newPassword: '' });

            const formatTime = (value) => {
                if (!value) return '-';
                const date = new Date(value);
                return Number.isNaN(date.getTime()) ? '-' : date.toLocaleString('zh-CN', { hour12: false });
            };

            const queryData = async () => {
                loading.value = true;
                try {
                    const response = await axios.get('/AdminUsers/List');
                    users.value = response.data || [];
                } catch (error) {
                    TDesign.MessagePlugin.error('加载失败');
                } finally {
                    loading.value = false;
                }
            };

            const postJson = async (url, data) => {
                return axios.post(url, data, {
                    headers: {
                        RequestVerificationToken: token
                    }
                });
            };

            const openCreate = () => {
                createForm.username = '';
                createForm.password = '';
                createVisible.value = true;
            };

            const submitCreate = async () => {
                if (!createForm.username || !createForm.password) {
                    TDesign.MessagePlugin.error('请输入管理员用户名和密码');
                    return;
                }
                saving.value = true;
                try {
                    const response = await postJson('/AdminUsers/Create', createForm);
                    if (response.data && response.data.success) {
                        TDesign.MessagePlugin.success('成功');
                        createVisible.value = false;
                        await queryData();
                    } else {
                        TDesign.MessagePlugin.error(response.data && response.data.message || '创建失败');
                    }
                } catch (error) {
                    TDesign.MessagePlugin.error('创建失败');
                } finally {
                    saving.value = false;
                }
            };

            const openReset = (row) => {
                resetForm.username = row.username;
                resetForm.newPassword = '';
                resetVisible.value = true;
            };

            const submitReset = async () => {
                if (!resetForm.username || !resetForm.newPassword) {
                    TDesign.MessagePlugin.error('请输入管理员用户名和新密码');
                    return;
                }
                saving.value = true;
                try {
                    const response = await postJson('/AdminUsers/ResetPassword', resetForm);
                    if (response.data && response.data.success) {
                        TDesign.MessagePlugin.success('成功');
                        resetVisible.value = false;
                    } else {
                        TDesign.MessagePlugin.error(response.data && response.data.message || '重置失败');
                    }
                } catch (error) {
                    TDesign.MessagePlugin.error('重置失败');
                } finally {
                    saving.value = false;
                }
            };

            const setEnabled = async (row, enabled) => {
                saving.value = true;
                try {
                    const response = await postJson('/AdminUsers/SetEnabled', {
                        username: row.username,
                        isEnabled: enabled
                    });
                    if (response.data && response.data.success) {
                        TDesign.MessagePlugin.success('成功');
                        row.isEnabled = enabled;
                    } else {
                        TDesign.MessagePlugin.error(response.data && response.data.message || '操作失败');
                    }
                } catch (error) {
                    TDesign.MessagePlugin.error('操作失败');
                } finally {
                    saving.value = false;
                }
            };

            const columns = [
                { colKey: 'username', title: '用户名', ellipsis: true },
                {
                    colKey: 'isEnabled',
                    title: '状态',
                    cell: (h, { row }) => row.isEnabled ? '启用' : '禁用'
                },
                {
                    colKey: 'createTime',
                    title: '创建时间',
                    cell: (h, { row }) => formatTime(row.createTime)
                },
                {
                    colKey: 'lastLoginTime',
                    title: '最近登录',
                    cell: (h, { row }) => formatTime(row.lastLoginTime)
                },
                {
                    colKey: 'actions',
                    title: '操作',
                    cell: (h, { row }) => h(TSpace, {}, {
                        default: () => [
                            h(TButton, { variant: 'text', theme: 'primary', onClick: () => openReset(row) }, { default: () => '重置密码' }),
                            h(TButton, {
                                variant: 'text',
                                theme: row.isEnabled ? 'danger' : 'success',
                                onClick: () => setEnabled(row, !row.isEnabled)
                            }, { default: () => row.isEnabled ? '禁用' : '启用' })
                        ]
                    })
                }
            ];

            Vue.onMounted(queryData);

            return {
                users,
                loading,
                saving,
                createVisible,
                resetVisible,
                createForm,
                resetForm,
                columns,
                queryData,
                openCreate,
                submitCreate,
                submitReset
            };
        }
    });

    app.use(TDesign);
    app.mount(el);
})();
