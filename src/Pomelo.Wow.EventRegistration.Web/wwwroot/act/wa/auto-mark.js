component.data = function () {
    return {
        activity: null,
        waString: null,
        arguments: [
            { Key: "GROUP_COUNT", Value: '0' },
            { Key: "GROUP_DEF", Value: '' },
            { Key: "NAMES_DEF", Value: '' }
        ],
        groups: [],
        raids: [],
        marks: [1, 2, 3, 4, 5, 6, 7, 8],
        enemies: []
    }
};

component.created = async function () {
    await this.loadRaids();
    var selectedRaids = this.activity.raids.split(',').map(x => parseInt(x.trim()));
    for (var i = 0; i < this.raids.length; ++i) {
        if (!selectedRaids.some(x => x == this.raids[i].id)) {
            continue;
        }

        var enemies = this.raids[i].enemies.split(',').map(x => x.trim());
        for (var j = 0; j < enemies.length; ++j) {
            this.enemies.push(enemies[j]);
        }
    }

    try {
        var groups = (await qv.get(`/api/guild/${this.activity.guildId}/var/auto_mark_${this.safeRaids(this.activity.raids)}`)).data;
        this.groups = groups;
        this.$forceUpdate();
    } catch (e) { }
};

component.mounted = function () {
    if (!this.groups.length) {
        this.addGroup();
    }

    this.generate();
};

component.methods = {
    addGroup: function () {
        this.groups.push({
            name: '新建分组',
            marks: [],
            names: []
        });
    },
    loadRaids: async function () {
        this.raids = (await qv.get('/api/raid')).data;
    },
    safeRaids: function (str) {
        return str.replaceAll(',', '_').replaceAll(' ', '_');
    },
    generate: async function () {
        if (!this.activity || this.taskIndex < 0) {
            alert('无法生成WA字符串');
            return;
        }

        if (app.guildPermission.guildManager) {
            await qv.post(`/api/guild/${this.activity.guildId}/var/auto_mark_${this.safeRaids(this.activity.raids)}`, {
                value: JSON.stringify(this.groups)
            });
        }
        this.waString = '正在生成...';

        // Prepare template models

        this.waString = await qv.post('//wa.mwow.org/api/wa/templates/auto-mark',
            {
                arguments: this.arguments
            }, 'text');
    },
    deleteGroup: function (i, name) {
        if (confirm(`你确定要删除${name}标记组吗？`)) {
            this.groups.splice(i, 1);
        }
    },
    toggleMark: function (group, markIndex) {
        if (group.marks.some(x => x == markIndex)) {
            var idx = group.marks.indexOf(markIndex);
            group.marks.splice(idx, 1);
        } else {
            group.marks.push(markIndex);
        }
    },
    toggleName: function (group, name) {
        if (group.names.some(x => x == name)) {
            var idx = group.names.indexOf(name);
            group.names.splice(idx, 1);
        } else {
            group.names.push(name);
        }
    }
};