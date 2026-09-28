// LifeHacks JSInterop Bridge
window.appInterop = {
    // 1. Feedback Tátil (Haptic)
    vibrate: function (pattern) {
        try {
            if ("vibrate" in navigator) {
                navigator.vibrate(pattern);
            }
        } catch (e) {
            console.warn("Haptics not supported or blocked", e);
        }
    },

    // 2. Web Notifications API
    requestNotificationPermission: async function () {
        if (!("Notification" in window)) {
            return "denied";
        }
        try {
            return await Notification.requestPermission();
        } catch (e) {
            return "denied";
        }
    },

    hasNotificationPermission: function () {
        return "Notification" in window && Notification.permission === "granted";
    },

    sendNotification: async function (title, body, tag) {
        if (!("Notification" in window) || Notification.permission !== "granted") {
            return;
        }

        try {
            if ("serviceWorker" in navigator) {
                const reg = await navigator.serviceWorker.ready;
                if (reg && reg.showNotification) {
                    await reg.showNotification(title, {
                        body: body,
                        icon: "icon-192.png",
                        badge: "icon-192.png",
                        tag: tag || "lifehacks-alert",
                        renotify: true,
                        vibrate: [200, 100, 200]
                    });
                    return;
                }
            }

            new Notification(title, {
                body: body,
                icon: "icon-192.png",
                tag: tag || "lifehacks-alert"
            });
        } catch (e) {
            console.warn("Notification error:", e);
        }
    },

    // 3. Download de Arquivos (Export de Backup JSON)
    downloadFile: function (fileName, contentType, content) {
        try {
            const blob = new Blob([content], { type: contentType });
            const url = URL.createObjectURL(blob);
            const a = document.createElement("a");
            a.href = url;
            a.download = fileName;
            document.body.appendChild(a);
            a.click();
            document.body.removeChild(a);
            URL.revokeObjectURL(url);
        } catch (e) {
            console.error("Failed to download file:", e);
        }
    },

    // 4. Celebração com Micro-Confetes via Canvas (leve, 60fps, sem dependências)
    triggerConfetti: function () {
        const canvas = document.createElement("canvas");
        canvas.style.position = "fixed";
        canvas.style.top = "0";
        canvas.style.left = "0";
        canvas.style.width = "100vw";
        canvas.style.height = "100vh";
        canvas.style.pointerEvents = "none";
        canvas.style.zIndex = "9999";
        document.body.appendChild(canvas);

        const ctx = canvas.getContext("2d");
        const width = (canvas.width = window.innerWidth);
        const height = (canvas.height = window.innerHeight);

        const confettiColors = ["#10B981", "#06B6D4", "#F59E0B", "#EC4899", "#8B5CF6", "#3B82F6"];
        const pieces = [];
        const count = 75;

        for (let i = 0; i < count; i++) {
            pieces.push({
                x: width * 0.5 + (Math.random() - 0.5) * 80,
                y: height * 0.4 + (Math.random() - 0.5) * 80,
                w: Math.random() * 8 + 6,
                h: Math.random() * 5 + 4,
                color: confettiColors[Math.floor(Math.random() * confettiColors.length)],
                vx: (Math.random() - 0.5) * 14,
                vy: (Math.random() - 0.7) * 16,
                rotation: Math.random() * 360,
                rotationSpeed: (Math.random() - 0.5) * 15,
                opacity: 1
            });
        }

        let frame = 0;
        function update() {
            frame++;
            ctx.clearRect(0, 0, width, height);

            let alive = false;
            for (let p of pieces) {
                p.x += p.vx;
                p.y += p.vy;
                p.vy += 0.35;
                p.vx *= 0.98;
                p.rotation += p.rotationSpeed;
                if (frame > 45) {
                    p.opacity -= 0.02;
                }

                if (p.opacity > 0 && p.y < height + 50) {
                    alive = true;
                    ctx.save();
                    ctx.translate(p.x, p.y);
                    ctx.rotate((p.rotation * Math.PI) / 180);
                    ctx.globalAlpha = Math.max(0, p.opacity);
                    ctx.fillStyle = p.color;
                    ctx.fillRect(-p.w / 2, -p.h / 2, p.w, p.h);
                    ctx.restore();
                }
            }

            if (alive && frame < 120) {
                requestAnimationFrame(update);
            } else {
                if (canvas.parentNode) {
                    canvas.parentNode.removeChild(canvas);
                }
            }
        }

        requestAnimationFrame(update);
    }
};
