component.data = function () {
    return {
        id: null,
        myCharactors: [],
        form: {
            newCharactor: {
                class: 1,
                name: '',
                realm: '',
                role: 0
            }
        },
        activity: null,
        raids: [],
        bosses: [],
        inProgress: false
    };
};

component.created = function () {
    app.active = 'activity';
    this.myCharactors = JSON.parse(window.localStorage.getItem('my_charactors') || '[]');
};

component.mounted = function () {
    this.loadActivity();
};

component.methods = {
    loadActivity: async function () {
        this.raids = (await qv.get('/api/raid')).data;
        var activity = (await qv.get('/api/activity/' + this.id)).data;
        $('title').html(activity.name);
        this.form.newCharactor.realm = activity.realm;
        this.bosses = this.getBossNames(activity.raids);
        for (let i = 0; i < activity.registrations.length; ++i) {
            activity.registrations[i].toggle = false;
            if (!activity.registrations[i].charactor) {
                continue;
            }
            var bossObj = activity.registrations[i].role == 2
                ? JSON.parse(activity.registrations[i].charactor.hpsBossRanks)
                : JSON.parse(activity.registrations[i].charactor.dpsBossRanks);
            activity.registrations[i].active = 'boss';
            activity.registrations[i].boss = bossObj;
            activity.registrations[i].bossPassed = this.getBossPassed(bossObj);
            activity.registrations[i].wcl = this.getWcl(bossObj);
            activity.registrations[i].items = [];
            activity.registrations[i].charactor.equipments = activity.registrations[i].charactor.equipments.split(',').map(x => x.trim());
        }
        var itemReq = activity.registrations
            .filter(x => x.charactor && x.charactor.equipments)
            .map(x => { return { group: x.id, ids: x.charactor.equipments.filter(x => x) } });
        var self = this;
        qv.post('/api/item/batch', { queries: itemReq }).then(data => {
            for (var i = 0; i < activity.registrations.length; ++i) {
                var fetched = data.data.filter(x => x.group == activity.registrations[i].id);
                if (!fetched.length) {
                    continue;
                }

                fetched = fetched[0];
                activity.registrations[i].items = fetched.items;
            }
            self.$forceUpdate();
        })
        this.activity = activity;
    },
    loadItemFor: async function (items, itemId) {
        items.push((await qv.get('/api/item/' + itemId)).data);
    },
    getItemColor: function (bossName, itemLevel) {
        for (var i = 0; i < this.raids.length; ++i) {
            if (this.raids[i].bossList.indexOf(bossName) >= 0) {
                if (itemLevel < this.raids[i].itemLevelEntrance) {
                    return 'gray';
                } else if (itemLevel < this.raids[i].itemLevelPreference && itemLevel >= this.raids[i].itemLevelEntrance) {
                    return 'green';
                } else if (itemLevel < this.raids[i].itemLevelGraduated && itemLevel >= this.raids[i].itemLevelPreference) {
                    return 'blue';
                } else {
                    return 'purple';
                }
            }
        }

        return 'gray';
    },
    getWcl: function (bossObj) {
        var wcl = 0.0;
        for (var i = 0; i < this.bosses.length; ++i) {
            if (bossObj.filter(x => x.Name == this.bosses[i]).length) {
                wcl += bossObj.filter(x => x.Name == this.bosses[i])[0].Parse;
            }
        }
        return wcl / this.bosses.length * 1.0;
    },
    getBossPassed: function (bossObj) {
        var passed = 0;
        for (var i = 0; i < this.bosses.length; ++i) {
            if (bossObj.filter(x => x.Name == this.bosses[i]).length) {
                ++passed;
            }
        }
        return passed;
    },
    getBossNames: function (raids) {
        var splited = raids.split(',').map(x => x.trim());
        var ret = [];
        for (var i = 0; i < splited.length; ++i) {
            var raid = this.raids.filter(x => x.id == splited[i]);
            if (!raid.length) {
                continue;
            }
            raid = raid[0];
            var splited2 = raid.bossList.split(',').map(x => x.trim());
            for (var j = 0; j < splited2.length; ++j) {
                ret.push(splited2[j]);
            }
        }
        return ret;
    },
    addCharactor: function () {
        if (!this.form.newCharactor.name) {
            alert('请输入角色名！');
        }
        this.myCharactors.push(JSON.parse(JSON.stringify(this.form.newCharactor)));
        window.localStorage.setItem('my_charactors', JSON.stringify(this.myCharactors));
        this.form.newCharactor = {
            class: 1,
            name: '',
            realm: '',
            role: 0
        };
    },
    deleteCharactor: function (ch, i) {
        if (confirm(`你确定要删除角色"${ch.name}"吗？`)) {
            this.myCharactors.splice(i);
            window.localStorage.setItem('my_charactors', JSON.stringify(this.myCharactors));
        }
    },
    register: async function (ch) {
        this.inProgress = true;
        await qv.post(`/api/activity/${this.id}/registrations`, {
            name: ch.name,
            role: ch.role,
            hint: ch.hint,
            class: ch.class
        });
        await this.loadActivity();
        this.inProgress = false;
        window.localStorage.setItem('my_charactors', JSON.stringify(this.myCharactors));
    },
    openItem: function (id) {
        window.open('https://cn.tbc.wowhead.com/item=' + id);
    },
    leave: async function (ch, takeLeave = true) {
        var reg = this.activity.registrations.filter(x => x.name == ch.name)[0];
        reg.status = takeLeave ? 3 : 0;
        await qv.patch('/api/activity/' + this.id + '/registrations/' + reg.id, { status: reg.status, role: reg.role });
        await this.loadActivity();
    },
    setStatus: async function (id, status, role) {
        await qv.patch('/api/activity/' + this.id + '/registrations/' + id, { status: status, role: role });
        await this.loadActivity();
    },
    deleteReg: async function (id) {
        if (!confirm("你确定要删除这个报名的角色吗？")) {
            return;
        }

        await qv.delete('/api/activity/' + this.id + '/registrations/' + id);
        await this.loadActivity();
    }
};