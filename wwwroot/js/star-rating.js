// Shared star-rating widget for Performance Evaluation.
//
// Interactive use: put a container with [data-star-input] and data-target
// pointing at a hidden input that holds the 0-5 value. Clicking a star writes
// the value and fires a 'change' event on the hidden input so score previews
// can recalculate.
//
// Read-only use: window.StarRating.render(value) returns star markup, used to
// fill in view modals that are built in JavaScript.
(function () {
    const MAX_STARS = 5;

    function paint(container, value) {
        container.querySelectorAll('.star').forEach(star => {
            const starValue = parseInt(star.dataset.value, 10);
            star.classList.toggle('filled', starValue <= value);
        });

        const label = container.parentElement
            ? container.parentElement.querySelector('.star-value-label')
            : null;
        if (label) {
            const weight = parseFloat(label.dataset.weight || '0');
            const points = weight ? (weight * value / MAX_STARS) : 0;
            label.textContent = value === 0
                ? 'Not rated'
                : `${value} / ${MAX_STARS} star${value === 1 ? '' : 's'}` +
                (weight ? ` \u2022 ${points.toFixed(2)} / ${weight} pts` : '');
        }
    }

    function wire(container) {
        if (container.dataset.starWired === 'true') return;
        container.dataset.starWired = 'true';

        const target = document.getElementById(container.dataset.target);
        if (!target) return;

        paint(container, parseInt(target.value || '0', 10));

        container.querySelectorAll('.star').forEach(star => {
            star.addEventListener('click', function () {
                if (container.classList.contains('readonly') || target.disabled) return;

                const clicked = parseInt(star.dataset.value, 10);
                // Clicking the star that's already the current value clears the
                // rating, so a mis-click is easy to undo.
                const current = parseInt(target.value || '0', 10);
                const next = (clicked === current) ? 0 : clicked;

                target.value = next;
                paint(container, next);
                target.dispatchEvent(new Event('change', { bubbles: true }));
            });
        });
    }

    function initAll(root) {
        (root || document).querySelectorAll('[data-star-input]').forEach(wire);
    }

    // Read-only star markup for a 0-5 value (accepts decimals; rounds to the
    // nearest whole star for display).
    function render(value, extraClass) {
        const rounded = Math.round(Math.max(0, Math.min(MAX_STARS, parseFloat(value) || 0)));
        let html = `<span class="star-rating readonly ${extraClass || 'sm'}">`;
        for (let i = 1; i <= MAX_STARS; i++) {
            html += `<span class="star${i <= rounded ? ' filled' : ''}"><i class="bi bi-star-fill"></i></span>`;
        }
        return html + '</span>';
    }

    window.StarRating = { initAll: initAll, render: render, MAX_STARS: MAX_STARS, paint: paint };

    document.addEventListener('DOMContentLoaded', function () { initAll(document); });
})();
