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
        bosses:[]
    };
};

component.created = function () {
    this.myCharactors = JSON.parse(window.localStorage.getItem('my_charactors') || '[]');
};

component.mounted = function () {
    this.loadActivity();
};

component.methods = {
    loadActivity: async function () {
        this.raids = (await qv.get('/api/raid')).data;
        var activity = (await qv.get('/api/activity/' + this.id)).data;
        this.bosses = this.getBossNames(activity.raids);
        for (var i = 0; i < activity.registrations.length; ++i) {
            if (!activity.registrations[i].charactor) {
                continue;
            }
            var bossObj = activity.registrations[i].role == 2
                ? JSON.parse(activity.registrations[i].charactor.hpsBossRanks)
                : JSON.parse(activity.registrations[i].charactor.dpsBossRanks);
            activity.registrations[i].boss = bossObj;
            activity.registrations[i].bossPassed = this.getBossPassed(bossObj);
            activity.registrations[i].wcl = this.getWcl(bossObj);
        }
        this.activity = activity;
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
        await qv.post(`/api/activity/${this.id}/registrations`, {
            name: ch.name,
            role: ch.role,
            hint: ch.hint,
            class: ch.class
        });
        await this.loadActivity();
    }
};