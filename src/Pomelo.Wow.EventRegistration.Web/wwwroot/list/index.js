component.data = function () {
    return {
        result: null
    };
};

component.created = function () {
    app.active = 'activity';
    this.load();
};

component.methods = {
    load: async function () {
        this.result = await qv.get(`/api/activity?page=1`);
    },
    next: async function () {
        ++this.result.currentPage;
        var result = await qv.get(`/api/activity?page=${this.result.currentPage + 1}`);
        for (var i = 0; i < result.data.length; ++i) {
            this.result.data.push(result.data[i]);
        }
    }
};