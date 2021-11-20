component.data = function () {
    return {
        activity: null,
        raids: [],
        selectedRaids: [],
    }
};

component.created = async function () {
    await this.loadRaids();
};

component.mounted = function () {
    this.selectedRaids = this.activity.raids.split(',').filter(x => x);
    $('#txt-begin').val(app.moment(new Date(this.activity.begin)).format('YYYY/MM/DD HH:00'));
    $('#txt-begin').datetimepicker();

    $('#txt-deadline').val(app.moment(new Date(this.activity.deadline)).format('YYYY/MM/DD HH:00'));
    $('#txt-deadline').datetimepicker();
};

component.methods = {
    loadRaids: async function () {
        this.raids = (await qv.get('/api/raid')).data;
    },
    patch: function () {
        this.activity.deadline = $('#txt-deadline').val();
        this.activity.begin = $('#txt-begin').val();
        this.activity.raids = this.selectedRaids.toString();
        qv.patch(`/api/activity/${this.activity.id}`, {
            name: this.activity.name,
            description: this.activity.description,
            begin: this.activity.begin,
            deadline: this.activity.deadline,
            estimatedDurationInHours: this.activity.estimatedDurationInHours,
            raids: this.activity.raids
        });
        alert('活动信息更新成功');
    }
};