// Behaviour for the "Recommendations" modal on Performance Evaluation
// Create/Edit. Mirrors the Fiber Plans modal on Job Ticket Create: add an entry
// with the Add button, remove one with its x button, both over AJAX against
// tbl_recommendation. The evaluation form itself is never submitted by any of
// this - only the dropdown's options are kept in sync.
(function () {
    const select = document.getElementById('recommendationSelect');
    const list = document.getElementById('recommendationList');
    const noneMsg = document.getElementById('noRecommendationsMsg');
    const input = document.getElementById('newRecommendationInput');
    const addBtn = document.getElementById('addRecommendationBtn');
    const errorBox = document.getElementById('recommendationModalError');

    if (!select || !list || !input || !addBtn) return;

    // Same restriction enforced server-side (Model.RecommendationRules.AllowedPattern).
    const allowedPattern = /^[A-Za-z0-9À-ÖØ-öø-ÿ.,\-\/#&()\s]+$/;
    const maxLength = 100;

    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';

    function showError(msg) {
        errorBox.textContent = msg;
        errorBox.classList.remove('d-none');
    }

    function clearError() {
        errorBox.classList.add('d-none');
        errorBox.textContent = '';
    }

    function addRow(id, name) {
        const li = document.createElement('li');
        li.className = 'list-group-item d-flex justify-content-between align-items-center';
        li.dataset.recId = id;
        li.dataset.recName = name;
        li.innerHTML = '<span></span><button type="button" class="btn btn-sm btn-link text-danger p-0 recommendation-delete-btn" title="Delete">&times;</button>';
        li.querySelector('span').textContent = name;
        list.appendChild(li);
        if (noneMsg) noneMsg.classList.add('d-none');
    }

    function addOption(name) {
        const opt = document.createElement('option');
        opt.value = name;
        opt.textContent = name;
        select.appendChild(opt);
    }

    addBtn.addEventListener('click', async function () {
        clearError();
        const name = input.value.trim();

        if (!name) {
            showError('Please enter a recommendation.');
            return;
        }
        if (name.length > maxLength) {
            showError('Recommendation is too long (max ' + maxLength + ' characters).');
            return;
        }
        if (!allowedPattern.test(name)) {
            showError('Only letters, numbers, spaces, and . , - / # & ( ) are allowed.');
            return;
        }

        addBtn.disabled = true;
        try {
            const body = new URLSearchParams();
            body.set('recommendationName', name);
            body.set('__RequestVerificationToken', token);

            const res = await fetch('?handler=AddRecommendation', {
                method: 'POST',
                headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
                body: body.toString()
            });
            const data = await res.json();

            if (!res.ok || !data.success) {
                showError(data.message || 'Could not add that recommendation.');
                return;
            }

            addRow(data.id, data.name);
            addOption(data.name);
            input.value = '';
        } catch (err) {
            showError('Something went wrong. Please try again.');
        } finally {
            addBtn.disabled = false;
        }
    });

    input.addEventListener('keydown', function (e) {
        if (e.key === 'Enter') {
            e.preventDefault();
            addBtn.click();
        }
    });

    list.addEventListener('click', async function (e) {
        const btn = e.target.closest('.recommendation-delete-btn');
        if (!btn) return;

        clearError();
        const li = btn.closest('li');
        const id = li.dataset.recId;
        const name = li.dataset.recName;

        btn.disabled = true;
        try {
            const body = new URLSearchParams();
            body.set('id', id);
            body.set('__RequestVerificationToken', token);

            const res = await fetch('?handler=DeleteRecommendation', {
                method: 'POST',
                headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
                body: body.toString()
            });
            const data = await res.json();

            if (!res.ok || !data.success) {
                showError(data.message || 'Could not delete that recommendation.');
                btn.disabled = false;
                return;
            }

            li.remove();
            if (!list.children.length && noneMsg) noneMsg.classList.remove('d-none');

            // If the entry being removed was selected on the form, clear the
            // selection so a deleted recommendation can't be submitted.
            const opt = Array.from(select.options).find(o => o.value === name);
            if (opt) {
                if (select.value === name) select.value = '';
                opt.remove();
            }
        } catch (err) {
            showError('Something went wrong. Please try again.');
            btn.disabled = false;
        }
    });
})();
