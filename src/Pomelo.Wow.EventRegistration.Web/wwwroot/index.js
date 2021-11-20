String.prototype.replaceAll = function (s1, s2) {
    return this.replace(new RegExp(s1, "gm"), s2);
}

var app = new Vue({
    data: {
        notifications: [],
        active: 'activity',
        user: {
            token: window.localStorage.getItem('token'),
            role: window.localStorage.getItem('role'),
            username: window.localStorage.getItem('username')
        },
        guildId: null,
        guild: null,
        guildPermission: {
            guildManager: false,
            guildOwner: false
        }
    },
    mounted: async function () {
        moment.locale('zh-cn');
        setInterval(function () {
            for (var i = 0; i < app.notifications.length; ++i) {
                if (app.notifications[i].closeTime > 0) {
                    --app.notifications[i].closeTime;
                }
                if (app.notifications[i].closeTime == 0) {
                    app.notifications.splice(i, 1);
                }
            }
        }, 1000);
        this.checkToken();
        var idx = window.location.host.indexOf('.mwow.org');
        if (idx > 0) {
            var len = window.location.host.length - '.mwow.org'.length;
            app.guildId = window.location.host.substr(0, len);
        }

        if (window.location.pathname != '/') {
            app.open(window.location.pathname + window.location.search);
        } else {
            if (app.guildId) {
                app.open('/home');
            } else {
                app.open('/guild');
            }
        }
    },
    methods: {
        checkToken: async function () {
            if (!this.user.token) {
                return;
            }

            var self = this;

            try {
                await qv.get('/api/user/someone/session/' + this.user.token);
            } catch (e) {
                self.user.token = null;
                self.user.role = null;
                self.user.username = null;
                token: window.localStorage.setItem('token', null);
                token: window.localStorage.setItem('role', null);
                token: window.localStorage.setItem('username', null);
            }
        },
        marked: function (text) {
            if (!text) return '';
            return marked(text);
        },
        getCurrentUrl: function () {
            return window.location.pathname + window.location.search;
        },
        newGuid: function () {
            return 'xxxxxxxx-xxxx-xxxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function (c) {
                var r = Math.random() * 16 | 0, v = c == 'x' ? r : (r & 0x3 | 0x8);
                return v.toString(16);
            });
        },
        notify: function (title, subtitle, color, closeTime, id) {
            var generateId = !id;
            id = id || this.newGuid();
            if (generateId) {
                this.notifications.push({ title: title, subtitle: subtitle, color: color, closeTime: closeTime, id: id });
            } else {
                var notification = this.notifications.filter(x => x.id == id);
                if (!notification.length) {
                    return null;
                }
                notification = notification[0];
                notification.title = title;
                notification.subtitle = subtitle;
                notification.color = color;
                notification.closeTime = closeTime;
            }
            return id;
        },
        closeNotification: function (id) {
            for (var i = 0; i < this.notifications.length; ++i) {
                if (this.notifications[i].id == id) {
                    this.notifications.splice(i, 1);
                    return;
                }
            }
        },
        open: async function (url, params, pushState = true) {
            if (this.$container.active) {
                this.$container.close(this.$container.active);
            }

            var splited = url.split('?');
            var _params = {};
            if (splited.length > 1) {
                var params = splited[1].split('&');
                for (let i = 0; i < params.length; ++i) {
                    let _splited = params[i].split('=');
                    _params[_splited[0]] = decodeURIComponent(_splited[1]);
                }
            }
            if (this.$container.active) {
                this.$container.close(this.$container.active);
            }
            this.child = await this.$container.open(splited[0], _params);
            if (pushState) {
                window.history.pushState(null, null, url);
            }
        },
        signOut: function () {
            window.sessionStorage.removeItem('user');
            window.sessionStorage.removeItem('token');
            window.location = '/login?url=' + encodeURIComponent(app.getCurrentUrl());
        },
        moment: function (date) {
            return moment(date);
        }
    },
    watch: {
        guildId: function () {
            var self = this;
            qv.get('/api/guild/' + self.guildId).then(data => {
                self.guild = data.data;
                $('title').html(self.guild.name);
                return qv.get('/api/user/permission');
            }).then(data => {
                self.guildPermission = data.data;
            });
        }
    }
});

var mainContainer = new PomeloComponentContainer('#main', app, app, function (view) {
}, function () { });

app.$container = mainContainer;
app.$mount('#app');

window.onpopstate = function (event) {
    if (app.$container.active) {
        app.$container.close(app.$container.active);
    }
    if (window.location.pathname !== '/') {
        app.open(window.location.pathname + window.location.search);
    }
};

$(window).click(function (e) {
    if ($(e.target).attr('vue-route') !== undefined) {
        let href = $(e.target).attr('href');
        window.history.pushState(null, null, href);
        app.open(href);
        e.preventDefault();
    }
    if ($(e.target).parents('[vue-route]').length > 0) {
        let href = $(e.target).parents('[vue-route]').attr('href');
        window.history.pushState(null, null, href);
        app.open(href);
        e.preventDefault();
    }
});

function sleep(ms) {
    return new Promise((res, rej) => {
        setTimeout(() => res());
    });
}