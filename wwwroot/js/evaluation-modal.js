// Fills the shared _EvaluationModal partial from the data-* attributes on any
// .view-evaluation-btn trigger. Depends on star-rating.js for star markup.
(function () {
    function escapeHtml(value) {
        return String(value ?? '').replace(/[&<>"']/g, c => ({
            '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;'
        }[c]));
    }

    function ratingBadgeClass(rating) {
        switch (rating) {
            case 'Excellent': return 'bg-success-subtle text-success-emphasis';
            case 'Very Good': return 'bg-primary-subtle text-primary-emphasis';
            case 'Good': return 'bg-info-subtle text-info-emphasis';
            case 'Needs Improvement': return 'bg-warning-subtle text-warning-emphasis';
            case 'Poor': return 'bg-danger-subtle text-danger-emphasis';
            default: return 'bg-secondary-subtle text-secondary-emphasis';
        }
    }

    function parseJson(raw) {
        try { return JSON.parse(raw || '[]'); } catch (e) { return []; }
    }

    function fill(btn) {
        const d = btn.dataset;
        const set = (id, text) => { const el = document.getElementById(id); if (el) el.textContent = text; };
        const setHtml = (id, html) => { const el = document.getElementById(id); if (el) el.innerHTML = html; };

        set('evalEmployee', d.employee || '-');
        set('evalRole', d.role || '-');
        set('evalPeriod', d.period || '-');
        set('evalDate', d.date || '-');
        set('evalEvaluator', d.evaluator || '-');
        set('evalScore', (d.overallScore || '0') + ' / 100');
        setHtml('evalStarsTop', window.StarRating.render((parseFloat(d.overallScore || '0')) / 20, ''));

        const ratingBadge = document.getElementById('evalRatingBadge');
        if (ratingBadge) {
            ratingBadge.textContent = d.rating || '-';
            ratingBadge.className = 'badge ' + ratingBadgeClass(d.rating);
        }

        const statusBadge = document.getElementById('evalStatusBadge');
        if (statusBadge) {
            statusBadge.textContent = d.status || '-';
            statusBadge.className = 'badge ' + (d.status === 'Finalized'
                ? 'bg-success-subtle text-success-emphasis'
                : 'bg-secondary-subtle text-secondary-emphasis');
        }

        // ---- Criteria ratings ----
        const results = parseJson(d.results);
        const body = document.getElementById('evalResultsBody');
        if (body) {
            if (results.length === 0) {
                body.innerHTML = '<tr><td colspan="4" class="text-muted small">No scored criteria.</td></tr>';
            } else {
                body.innerHTML = results.map(r => `
                    <tr>
                        <td class="small">${escapeHtml(r.CriteriaName)}<div class="text-muted" style="font-size:.75rem">Weight ${r.Weight}%</div></td>
                        <td>${window.StarRating.render(r.Stars)}</td>
                        <td class="small">${r.Score} / ${r.Weight}</td>
                        <td class="small">${r.Feedback ? escapeHtml(r.Feedback) : '<span class="text-muted">-</span>'}</td>
                    </tr>`).join('');
            }
        }

        // ---- Appraisal recommendations ----
        const recs = parseJson(d.recommendations);
        const recBox = document.getElementById('evalRecommendations');
        if (recBox) {
            if (recs.length === 0) {
                recBox.innerHTML = '<div class="text-muted small">No recommendation was added for this evaluation.</div>';
            } else {
                recBox.innerHTML = recs.map(r => `
                    <div class="d-flex align-items-start gap-2 mb-2">
                        <span class="badge bg-${r.Badge}-subtle text-${r.Badge}-emphasis">${escapeHtml(r.Label)}</span>
                        <span class="small text-muted">${r.Details ? escapeHtml(r.Details) : ''}</span>
                    </div>`).join('');
            }
        }

        const feedback = document.getElementById('evalFeedback');
        if (feedback) {
            feedback.textContent = d.feedback && d.feedback.trim() ? d.feedback : 'No feedback recorded.';
        }
    }

    document.addEventListener('click', function (e) {
        const btn = e.target.closest('.view-evaluation-btn');
        if (btn) fill(btn);
    });
})();
