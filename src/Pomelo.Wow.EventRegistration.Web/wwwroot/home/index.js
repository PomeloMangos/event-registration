component.data = function () {
    return {
        result: null,
        active: 'activity'
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
        this.result = await qv.get(`/api/activity?page=1`);
    },
    next: async function () {
        ++this.result.currentPage;
        var result = await qv.get(`/api/activity?page=${this.result.currentPage}`);
        for (var i = 0; i < result.data.length; ++i) {
            this.result.data.push(result.data[i]);
        }
    }
};