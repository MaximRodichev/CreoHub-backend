if (!Array.isArray) {
    Array.isArray = function(value) {
        return Object.prototype.toString.call(value) === "[object Array]";
    };
}
if (!Array.prototype.map) {
    Array.prototype.map = function(callback, thisArg) {
        var result = [];
        for (var i = 0; i < this.length; i++) {
            if (i in this) {
                result.push(callback.call(thisArg, this[i], i, this));
            }
        }
        return result;
    };
}
if (!Array.prototype.indexOf) {
    Array.prototype.indexOf = function(searchElement, fromIndex) {
        var len = this.length >>> 0; // Ensure the array length is a 32-bit integer
        var from = Number(fromIndex) || 0;

        from = (from < 0) ? Math.max(len + from, 0) : from;

        for (var i = from; i < len; i++) {
            if (this[i] === searchElement) {
                return i;
            }
        }
        return -1;
    };
}