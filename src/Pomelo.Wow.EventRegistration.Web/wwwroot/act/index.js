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
        activity: null
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
        this.activity = (await qv.get('/api/activity/' + this.id)).data;
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