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
        var page = 1;
        if (this.result) {
            page = this.result.currentPage + 2;
        }
        this.result = await qv.get(`/api/activity?page=${page}`);
    }
};