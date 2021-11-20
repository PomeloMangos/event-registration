component.data = function () {
    return {
        active: 'activity'
    };
};

component.created = function () {
    app.active = 'activity';
};

component.mounted = function () {
    this.$container = new PomeloComponentContainer('#guild-container', app, this, function (view) {
    }, function () { });
    this.$container.open(`/guild/${this.active}`);

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

component.watch = {
    active: function () {
        this.$container.open(`/guild/${this.active}`);
    }
};
