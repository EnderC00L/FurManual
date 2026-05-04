export const IP_REGEX = /^(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$/;

export const DEVICE_CONFIG = {
    pc: { name: 'PC', ports: ['FastEthernet0'], color: '#334155', icon: '#icon-pc', layer: 3, hasIp: true },
    switch: { name: 'Switch', ports: Array.from({ length: 6 }, (_, i) => `Fa0/${i + 1}`), color: '#0369a1', icon: '#icon-switch', layer: 2, hasIp: false },
    router: { name: 'Router', ports: ['Gi0/0', 'Gi0/1'], color: '#ea580c', icon: '#icon-router', layer: 3, hasIp: true },
};

export function ipToLong(ip) {
    return ip.split('.').reduce((acc, octet) => (acc << 8) + parseInt(octet, 10), 0) >>> 0;
}

export function longToIp(long) {
    return [
        (long >>> 24) & 255,
        (long >>> 16) & 255,
        (long >>> 8) & 255,
        long & 255,
    ].join('.');
}

export function getMaskLength(maskStr) {
    if (!maskStr) return NaN;
    if (maskStr.includes('.')) {
        const maskLong = ipToLong(maskStr);
        let len = 0;
        for (let i = 31; i >= 0; i--) {
            if ((maskLong & (1 << i)) !== 0) len++;
            else break;
        }
        return len;
    }
    return parseInt(maskStr.replace('/', ''), 10);
}

export function validateCable(typeA, typeB, cable) {
    const layerA = DEVICE_CONFIG[typeA].layer;
    const layerB = DEVICE_CONFIG[typeB].layer;
    const needCross = (layerA === layerB) ||
        (typeA === 'pc' && typeB === 'router') ||
        (typeA === 'router' && typeB === 'pc');
    return (needCross && cable === 'cable_cross') || (!needCross && cable === 'cable_straight');
}

export function findPathBFS(start, end, devices, connections) {
    const queue = [[start]];
    const visited = new Set([start]);
    while (queue.length > 0) {
        const path = queue.shift();
        const node = path[path.length - 1];
        if (node === end) return path;
        for (const c of connections) {
            if (!c.status) continue;
            let neighbor = null;
            if (c.from === node) neighbor = c.to;
            else if (c.to === node) neighbor = c.from;
            if (!neighbor) continue;
            if (devices[neighbor].type === 'router' && !devices[neighbor].ip) continue;
            if (!visited.has(neighbor)) {
                visited.add(neighbor);
                queue.push([...path, neighbor]);
            }
        }
    }
    return null;
}

export function readCookie(name) {
    const eq = `${name}=`;
    const parts = document.cookie.split(';');
    for (let part of parts) {
        part = part.trim();
        if (part.startsWith(eq)) return decodeURIComponent(part.slice(eq.length));
    }
    return null;
}

export function writeCookie(name, value, days) {
    let expires = '';
    if (days) {
        const d = new Date();
        d.setTime(d.getTime() + days * 864e5);
        expires = `; expires=${d.toUTCString()}`;
    }
    document.cookie = `${name}=${encodeURIComponent(value || '')}${expires}; path=/; SameSite=Lax`;
}
