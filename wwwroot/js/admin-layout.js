(function () {
    const el = document.getElementById('admin-layout-app');
    if (!el || el.dataset.superAdmin !== 'true' || !window.Vue || !window.TDesign) return;
    const verificationToken = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

    const app = Vue.createApp({
        setup() {
            const resetVisible = Vue.ref(false);
            const saving = Vue.ref(false);
            const form = Vue.reactive({
                username: '',
                newPassword: ''
            });

            const openReset = () => {
                form.username = '';
                form.newPassword = '';
                resetVisible.value = true;
            };

            const submitReset = async () => {
                if (!form.username || !form.newPassword) {
                    TDesign.MessagePlugin.error('请输入管理员用户名和新密码');
                    return;
                }

                saving.value = true;
                try {
                    const response = await axios.post('/Account/ResetAdminPassword', {
                        username: form.username,
                        newPassword: form.newPassword
                    }, {
                        headers: {
                            RequestVerificationToken: verificationToken
                        }
                    });
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

            return { resetVisible, saving, form, openReset, submitReset };
        }
    });
    app.use(TDesign);
    app.mount(el);
})();
