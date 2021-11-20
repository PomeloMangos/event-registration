component.data = function () {
    return {
        result: null,
        active: 'activity',
        prices: [],
        form: {
            createPrice: {
                name: '',
                data: '[]'
            },
            createGroup: {
                name: ''
            },
            createItem: {
                itemId: null,
                initial: null,
                cap: null
            }
        },
        working: false,
        price: {},
        status: 0
    };
};

component.created = function () {
    app.active = 'activity';
    this.load();
};

component.mounted = function () {
    var calendarBl = $('.calendar-bl');
    if (calendarBl.length) {
        calendarBl.dateRangePicker({
            language: 'cn',
            inline: true,
            container: '.calendar-container',
            alwaysOpen: true,
            singleDate: true,
            singleMonth: true,
            showTopbar: false,
            customArrowPrevSymbol: '<i class="fa fa-angle-left fsize-14"></i>',
            customArrowNextSymbol: '<i class="fa fa-angle-right fsize-14"></i>'
        });
    }
}

component.methods = {
    load: async function () {
        this.result = await qv.get(`/api/activity?page=1&status=${this.status}`);
    },
    next: async function () {
        ++this.result.currentPage;
        var result = await qv.get(`/api/activity?page=${this.result.currentPage}&status=${this.status}`);
        for (var i = 0; i < result.data.length; ++i) {
            this.result.data.push(result.data[i]);
        }
    },
    loadPrices: async function () {
        this.prices = (await qv.get(`/api/guild/${app.guildId}/price`)).data;
    },
    createPrice: async function () {
        try {
            await qv.post(`/api/guild/${app.guildId}/price`, this.form.createPrice);
            this.loadPrices();
            alert('价目表创建成功');
        } catch (e) {
            alert('价目表创建失败');
        }
    },
    openPrice: async function (id) {
        var price = (await qv.get(`/api/guild/${app.guildId}/price/${id}`)).data;
        price.data = JSON.parse(price.data);
        this.price = price;
        this.$forceUpdate();
        this.active = 'price-detail'
    },
    createGroup: function () {
        this.price.data.push({
            name: this.form.createGroup.name,
            items: []
        });

        this.form.createGroup.name = '';
    },
    createItem: async function (group) {
        this.working = true;
        try {
            var result = await qv.get('/api/item/' + this.form.createItem.itemId);
            var item = result.data;
            item.initial = this.form.createItem.initial;
            item.cap = this.form.createItem.cap;
            group.items.push(item);
            this.working = false;

            this.form.createItem = {
                itemId: null,
                initial: null,
                cap: null
            };
        } catch (e) {
            console.error(e);
            alert('物品添加失败');
            this.working = false;
        }
    },
    openItem: function (id) {
        window.open('https://cn.tbc.wowhead.com/item=' + id);
    },
    removeItem: function (group, i) {
        group.items.splice(i, 1);
    },
    patchPrice: async function () {
        try {
            await qv.patch(`/api/guild/${app.guildId}/price/${this.price.id}`, {
                name: this.price.name,
                data: JSON.stringify(this.price.data)
            });
            alert('价目表保存成功');
        } catch (e) {
            console.error(e);
            alert('价目表保存失败');
        }
    }
};

component.watch = {
    active: function () {
        if (this.active == 'price') {
            this.loadPrices();
        }
    },
    status: function () {
        this.load();
    }
};