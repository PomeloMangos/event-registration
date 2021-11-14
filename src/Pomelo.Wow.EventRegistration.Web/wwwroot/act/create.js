component.data = function () {
    return {
        form: {
            name: '',
            server: 0,
            realm: app.guild.realm,
            description: '',
            deadline: null,
            begin: null,
            raids: '',
            estimatedDuration: 4.5
        },
        selectedRaids: [],
        raids: []
    };
};

component.created = function () {
    app.active = 'activity';
    this.loadRaids();
};

component.mounted = function () {
    var self = this;
    $('#txt-begin').val(app.moment(new Date()).format('YYYY/MM/DD HH:00'));
    $('#txt-begin').datetimepicker();

    $('#txt-deadline').val(app.moment(new Date()).format('YYYY/MM/DD HH:00'));
    $('#txt-deadline').datetimepicker();
};

component.methods = {
    create: async function () {
        if (!this.form.name) {
            alert('活动名称不能为空');
            return;
        }

        if (!this.selectedRaids.length) {
            alert('请至少选择一个副本');
            return;
        }

        if (!this.form.realm) {
            alert('请填写活动所在服务器名称');
            return;
        }

        this.form.begin = new Date($('#txt-begin').val()).toISOString();
        this.form.deadline = new Date($('#txt-deadline').val()).toISOString();
        this.form.raids = this.selectedRaids.toString();
        var result = await qv.post('/api/activity', this.form);
        if (result.code != 200) {
            alert('活动创建失败');
            return;
        }

        app.open('/act?id=' + result.data.id);
    },
    loadRaids: async function () {
        this.raids = (await qv.get('/api/raid')).data;
    }
};