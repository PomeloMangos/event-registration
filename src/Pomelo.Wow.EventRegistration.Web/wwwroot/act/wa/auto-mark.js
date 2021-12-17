component.data = function () {
    return {
        activity: null,
        waString: null,
        arguments: [
            { Key: "GROUP_COUNT", Value: 0 },
            { Key: "GROUP_DEF", Value: '' },
            { Key: "NAMES_DEF", Value: '' }
        ],
        groups: [],
        raids : []
    }
};

component.created = async function () {
    this.raids = this.$parent.$parent.raids;
};

component.mounted = function () {
    this.generate();
};

component.methods = {
    generate: async function () {
        if (!this.activity || this.taskIndex < 0) {
            alert('无法生成WA字符串');
            return;
        }

        if (app.guildPermission.guildManager) {
            // TODO: Save
        }
        this.waString = '正在生成...';

        // Prepare template models

        this.waString = await qv.post('//wa.mwow.org/api/wa/templates/auto-mark',
            {
                arguments: this.arguments
            }, 'text');
    },
    saveTasks: function () {
        qv.patch(`/api/activity/${this.activity.id}`, { extension2: JSON.stringify(this.tasks) });
    }
};