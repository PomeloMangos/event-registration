﻿component.data = function () {
    return {
        active: null,
        activity: null
    };
};

component.mounted = function () {
    this.$container = new PomeloComponentContainer('#act-manage-inner-container', app, this, function (view) {
    }, function () { });
    this.open('common');
};

component.methods = {
    open: function (view) {
        this.active = view;
        this.$container.open('/act/manage/' + view, { activity: this.activity });
    }
};