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
        }
    };
};

component.created = function () {
    this.myCharactors = JSON.parse(window.localStorage.getItem('my_charactors') || '[]');
};

component.methods = {
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
    }
};