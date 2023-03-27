const CORS_HEADERS = {
    credentials: 'include',
    mode: 'cors',
    headers: {
        'Access-Control-Allow-Origin': '*'
    }
};

(function () {
    let app = null;
    class App {
        constructor(data) {
            Object.assign(this, data);
            app = this;

            this.isAuthenticated = ko.observable(data.isAuthenticated);
            this.email = ko.observable();
            this.password = ko.observable();
            this.affiliates = (data.affiliates || []).map(a => new vmAffiliate(a));
            this.posts = (data.posts || []).map(p => new vmPost(p));
        }

        login() {
            var data = {
                email: this.email(),
                password: this.password()
            };
            api_post('/api/v1/login', data)
                .then(res => {
                    this.isAuthenticated(true);
                    this.affiliates.forEach(aff => {
                    //    aff.preAuthAttempts = 1;
                    //    aff.updatePreAuth();
                    });
                });
        }

        logout() {
            this.isAuthenticated(false);
            api_post('/api/v1/logout', {});

            this.affiliates.forEach(a => {
                api_post(`${a.baseUrl}/api/v1/logout`, CORS_HEADERS);
            });
        }
    }

    class vmAffiliate {
        constructor(data) {
            Object.assign(this, data);

            this.loginUrl = ko.observable();
        }

        get redirectUri() { return `${app.serverUri}/oauth/callback` }

        async signIn() {
            const req = new OpenAuthRequest(this);
            this.auth = await req.signIn();

            attemptAuthentication('', this.auth);
            app.affiliates
                .filter(a => a !== this)
                .forEach(a => attemptAuthentication(a.baseUrl, this.auth));

            app.isAuthenticated(!!this.auth);
        }

        async attemptAuthentication(auth) {
            if (auth) {
                const authResponse = await fetch(`${this.baseUrl}/oauth/authenticate`, {
                    credentials: 'include',
                    mode: 'cors',
                    headers: {
                        'Access-Control-Allow-Origin': '*',
                        'Authorization': `${auth.token_type} ${auth.access_token}`
                    }
                });
            }
        }

        closeAuthFrame(url) {
            if (this.loginCallback) {
                this.loginCallback(url);
            }

            this.loginUrl('');
        }
    }

    class vmPost {
        constructor(data) {
            Object.assign(this, data);
        }

        openPost() {

        }
    }

    async function attemptAuthentication(baseUrl, auth) {
        if (auth) {
            const authResponse = await fetch(`${baseUrl}/oauth/authenticate`, {
                credentials: 'include',
                mode: 'cors',
                headers: {
                    'Access-Control-Allow-Origin': '*',
                    'Authorization': `${auth.token_type} ${auth.access_token}`
                }
            });
        }
    }

    let access_token = null;
    async function getAccessToken() {
        try {
            if (!access_token) {
                const res = await fetch('/oauth/token');
                const token = await res.json();
                access_token = token.access_token;
            }

            return access_token;
        }
        catch(ex) {
            access_token = null;
            app.isAuthenticated(false);
        }
    }

    ko.bindingHandlers.monitorAuth = {
        init: function (el, acc, binding, ctx) {
        //    ctx.preAuthAttempts = 1;
        //    ctx.updatePreAuth();
        }
    };

    ko.bindingHandlers.hidden = {
        init: function (el, acc) {
            const variable = acc();

            if (ko.isObservable(variable)) {
                variable.subscribe(onUpdate);
                onUpdate(variable());
            }
            else {
                onUpdate(variable);
            }

            function onUpdate(hidden) {
                if (hidden) {
                    el.classList.add('hidden');
                }
                else {
                    el.classList.remove('hidden');
                }
            }
        }
    };

    window.App = App;
})();

(function () {
    class OpenAuthRequest {
        constructor(affiliate) {
            this.affiliate = affiliate;
        }

        get loginUrl() { return this.affiliate.loginUrl; }
        get baseUrl() { return this.affiliate.baseUrl; }
        get redirectUri() { return this.affiliate.redirectUri; }

        async signIn() {
            this.codeVerifier = generateCodeVerifier();
            return await this.sendAuthRequest();
        }

        async sendAuthRequest() {
            const query = {
                response_type: 'code',
                client_id: '29352735982374239857',
                redirect_uri: this.redirectUri,
                scope: 'create delete',
                state: '000',

                code_challenge: await generateCodeChallengeFromVerifier(this.codeVerifier),
                code_challenge_method: 'S256',
            };

            const authResponse = await fetch(`${this.baseUrl}/oauth/authorize?${toQueryString(query)}`, CORS_HEADERS);
            if (authResponse.redirected) {
                if (authResponse.url.startsWith(query.redirect_uri)) {
                    return await this.processCallbackUrl(authResponse.url);
                }
                else {
                    return await this.attemptLogin(authResponse.url);
                }
            }
        }

        async processCallbackUrl(callbackUrl) {
            const qString = callbackUrl.substring(callbackUrl.indexOf('?') + 1);
            const qParams = qString.split('&').reduce((o, it) => {
                const idx = it.indexOf('=');
                const key = it.substring(0, idx);
                const value = decodeURIComponent(it.substring(idx + 1));
                o[key] = value;

                return o;
            }, {});

            return await this.sendTokenRequest(qParams);
        }

        async sendTokenRequest(qParams) {
            const tokenRequest = {
                'grant_type': "authorization_code",
                'code_verifier': this.codeVerifier,
                'redirect_url': this.redirectUri,
                'code': qParams.code,
            };

            let authResponse = await fetch(qParams.iss, {
                method: 'POST',
                credentials: 'include',
                mode: 'cors',
                headers: {
                    'Access-Control-Allow-Origin': '*',
                    'Content-Type': 'application/x-www-form-urlencoded'
                },
                body: Object.keys(tokenRequest)
                    .map(k => `${k}=${tokenRequest[k]}`)
                    .join('&')
            });

            if (authResponse.ok) {
                return await authResponse.json();
            }
        }

        async attemptLogin(url) {
            const promise = new Promise((cb, err) => this.affiliate.loginCallback = cb);

            this.loginUrl(url);

            const callbackUrl = await promise;
            if (callbackUrl) {
                return await this.processCallbackUrl(callbackUrl);
            }

            this.affiliate.loginCallback = undefined;
        }
    }

    function generateCodeVerifier() {
        function dec2hex(dec) {
            return ("0" + dec.toString(16)).substr(-2);
        }

        const array = new Uint32Array(56 / 2);
        window.crypto.getRandomValues(array);
        return Array.from(array, dec2hex).join("");
    }

    async function generateCodeChallengeFromVerifier(v) {
        function sha256(plain) {
            // returns promise ArrayBuffer
            const encoder = new TextEncoder();
            const data = encoder.encode(plain);

            return window.crypto.subtle.digest("SHA-256", data);
        }

        function base64urlencode(a) {
            let str = "";
            const bytes = new Uint8Array(a);
            const len = bytes.byteLength;
            for (var i = 0; i < len; i++) {
                str += String.fromCharCode(bytes[i]);
            }
            return btoa(str)
                .replace(/\+/g, "-")
                .replace(/\//g, "_")
                .replace(/=+$/, "");
        }

        const hashed = await sha256(v);
        const base64encoded = base64urlencode(hashed);
        return base64encoded;
    }

    window.addEventListener('message', ev => {
        const iframe = Array.from(document.body.querySelectorAll('iframe'))
            .filter(i => i.contentWindow === ev.source)[0];
        const affiliate = ko.dataFor(iframe);

        if (affiliate) {
            switch (ev.data?.action) {
                case 'closeAuthFrame':
                    affiliate.closeAuthFrame(ev.source.location.href);
                    break;
            }
        }
    });

    window.OpenAuthRequest = OpenAuthRequest;
})();

function toQueryString(obj) {
    return Array.from(Object.keys(obj))
        .map(k => `${k}=${encodeURIComponent(obj[k])}`)
        .join('&');
}
