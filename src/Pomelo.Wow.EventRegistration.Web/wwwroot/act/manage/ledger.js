component.data = function () {
    return {
        activity: null,
        ledgerString: null,
        ledger: {
            income: [],
            expense: [],
            other: []
        },
        busy: false
    }
};
component.created = async function () {
};

component.mounted = function () {
};

component.methods = {
    importLedger: async function () {
        if (this.busy) {
            return;
        }

        this.busy = true;

        this.ledger = {
            income: [],
            expense: [],
            other: []
        };

        try {
            var rows = this.ledgerString.split('\n').map(x => x.trim());
            for (var i = 0; i < rows.length; ++i) {
                var cols = rows[i].split(',');
                if (cols.length < 5) {
                    continue;
                }

                if (cols[1].indexOf('ItemId:') > 0) {
                    var itemId = cols[1].substr(cols[1].indexOf('ItemId:') + 'ItemId:'.length);
                    itemId = itemId.substr(0, itemId.length - 1);
                    this.ledger.income.push({
                        name: cols[1].substr(0, cols[1].indexOf('(')),
                        itemId: parseInt(itemId),
                        amount: parseInt(cols[2]),
                        player: cols[3],
                        price: parseInt(cols[4]),
                        item: null
                    });
                } else if (cols[0] == '收入') {
                    this.ledger.other.push({
                        name: cols[1],
                        amount: parseInt(cols[2]),
                        player: cols[3],
                        price: parseInt(cols[4])
                    });
                } else {
                    this.ledger.expense.push({
                        name: cols[1],
                        amount: parseInt(cols[2]),
                        player: cols[3],
                        price: parseInt(cols[4])
                    });
                }
            }

            var items = (await qv.post('/api/item/batch', {
                queries: [{
                    group: '1',
                    ids: this.ledger.income.map(x => x.itemId)
                }]
            })).data[0].items;

            for (var i = 0; i < this.ledger.income.length; ++i) {
                var income = this.ledger.income[i];
                if (!items.some(x => x.id == income.itemId)) {
                    continue;
                }

                income.item = items.filter(x => x.id == income.itemId)[0];
            }

            await this.saveLedger();
        } catch (e) {
            console.error(e);
            this.busy = false;
            alert('账本导入失败');
        }
        
    },
    saveLedger: async function () {
        await qv.patch(`/api/activity/${this.activity.id}`, { extension3: JSON.stringify(this.ledger) });
        this.$parent.$parent.ledger = this.ledger;
        alert("账本导入成功");
    }
};

component.watch = {
};
