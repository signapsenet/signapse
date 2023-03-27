async function api(method, path, data) {
    try {
        if (path.indexOf('http') !== 0 && path[0] !== '/') {
            path = '/api/v1/' + path;
        }

        const req = {
            method: method || 'GET', // *GET, POST, PUT, DELETE, etc.
            mode: 'cors', // no-cors, *cors, same-origin
            cache: 'no-cache', // *default, no-cache, reload, force-cache, only-if-cached
            credentials: 'same-origin', // include, *same-origin, omit
            headers: {
                'Content-Type': 'application/json'
                // 'Content-Type': 'application/x-www-form-urlencoded',
            },
            redirect: 'follow', // manual, *follow, error
            referrerPolicy: 'no-referrer', // no-referrer, *no-referrer-when-downgrade, origin, origin-when-cross-origin, same-origin, strict-origin, strict-origin-when-cross-origin, unsafe-url
        };

        if (method !== 'GET' && method !== 'HEAD') {
            req.body = JSON.stringify({ data });
        }

        const response = await fetch(path, req);
        if (response.ok && response.headers.get('Content-type')?.startsWith('application/json')) {
            const json = await response.json();
            return json.result ?? json;
        }
    }
    catch (ex) {
        console.error(ex);
    }
}

const api_get = async (path, data) => api('GET', path, data);
const api_put = async (path, data) => api('PUT', path, data);
const api_post = async (path, data) => api('POST', path, data);

function docReady(fn) {
    // see if DOM is already available
    if (document.readyState === "complete" || document.readyState === "interactive") {
        // call on next available tick
        setTimeout(fn, 1);
    } else {
        document.addEventListener("DOMContentLoaded", fn);
    }
}

// Avoid submitting forms when we click buttons with a submit type. This allows us to
// more cleanly handle forms when js is disabled in the browser.
(function (origClickHandler) {
    const origInit = origClickHandler.init;

    ko.bindingHandlers.click.init = function (element) {
        if (element.nodeName === 'BUTTON' && element.getAttribute('type') === 'submit') {
            element.setAttribute('type', 'button');
        }

        return origInit.apply(this, arguments);
    };
}) (ko.bindingHandlers.click);