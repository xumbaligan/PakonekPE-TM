// Charts for the Manager Dashboard (Pages/Index).
//
// The data comes from the <script type="application/json"> blocks the page
// renders (one for Office Tasks, one for Job Tickets), so nothing here
// depends on Razor interpolating values into JavaScript. Same conventions as
// performance-report-charts.js: every failure path shows a message inside
// the chart card instead of leaving a blank canvas.
(function () {
    function showMessage(id, text) {
        const el = document.getElementById(id);
        if (!el) return;
        el.textContent = text;
        el.classList.remove('d-none');

        const canvas = el.parentElement.querySelector('canvas');
        if (canvas) canvas.classList.add('d-none');
    }

    // Same status colours used across the Job Tickets / Office Task pages
    // (badge classes), so a status reads the same way everywhere a manager
    // sees it.
    const statusColors = {
        'Pending': '#6c757d',
        'In Progress': '#0dcaf0',
        'Completed': '#198754',
        'Overdue': '#dc3545',
        'Cancelled': '#6f42c1',
        'Reschedule Request': '#ffc107',
        'Rescheduled': '#fd7e14'
    };
    const fallbackColor = '#adb5bd';

    function parsePayload(dataElId, messageIds) {
        const dataEl = document.getElementById(dataElId);
        if (!dataEl) return null;

        try {
            return JSON.parse(dataEl.textContent);
        } catch (err) {
            console.error('Dashboard: could not parse chart data (' + dataElId + ').', err);
            messageIds.forEach(id => showMessage(id, 'Chart data could not be read.'));
            return null;
        }
    }

    const emptyMsg = 'Nothing recorded yet, so there is nothing to chart.';

    // Pie chart: status breakdown. `noun` is used in the tooltip, e.g.
    // "3 tickets (30%)" vs "3 tasks (30%)".
    function renderStatusPie(statusData, canvasId, messageId, noun) {
        if (statusData.length === 0) {
            showMessage(messageId, emptyMsg);
            return;
        }

        new Chart(document.getElementById(canvasId), {
            type: 'pie',
            data: {
                labels: statusData.map(d => d.Label),
                datasets: [{
                    data: statusData.map(d => Number(d.Value)),
                    backgroundColor: statusData.map(d => statusColors[d.Label] || fallbackColor),
                    borderColor: '#fff',
                    borderWidth: 2
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    // Percentages sit right on the legend so a manager can read
                    // the status breakdown at a glance.
                    legend: {
                        position: 'bottom',
                        labels: {
                            generateLabels: function (chart) {
                                const data = chart.data;
                                const values = data.datasets[0].data;
                                const total = values.reduce((a, b) => a + b, 0);
                                return data.labels.map(function (label, i) {
                                    const pct = total ? Math.round(values[i] * 100 / total) : 0;
                                    return {
                                        text: label + ' — ' + pct + '%',
                                        fillStyle: data.datasets[0].backgroundColor[i],
                                        strokeStyle: data.datasets[0].borderColor,
                                        lineWidth: data.datasets[0].borderWidth,
                                        index: i
                                    };
                                });
                            }
                        }
                    },
                    tooltip: {
                        callbacks: {
                            label: function (ctx) {
                                const total = ctx.dataset.data.reduce((a, b) => a + b, 0);
                                const pct = total ? Math.round(ctx.parsed * 100 / total) : 0;
                                const label = ctx.parsed === 1 ? noun.replace(/s$/, '') : noun;
                                return ctx.label + ': ' + ctx.parsed + ' ' + label + ' (' + pct + '%)';
                            }
                        }
                    }
                }
            }
        });
    }

    // Line chart: count created per month. `datasetLabel` names the series,
    // `noun` is used in the tooltip singular/plural.
    function renderOverTimeLine(overTimeData, canvasId, messageId, datasetLabel, noun) {
        if (overTimeData.length === 0) {
            showMessage(messageId, emptyMsg);
            return;
        }

        new Chart(document.getElementById(canvasId), {
            type: 'line',
            data: {
                labels: overTimeData.map(d => d.Label),
                datasets: [{
                    label: datasetLabel,
                    data: overTimeData.map(d => Number(d.Value)),
                    borderColor: '#0d6efd',
                    backgroundColor: 'rgba(13, 110, 253, 0.15)',
                    fill: true,
                    tension: 0.3,
                    pointRadius: 4,
                    pointBackgroundColor: '#0d6efd'
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: { display: false },
                    tooltip: {
                        callbacks: {
                            label: function (ctx) {
                                const label = ctx.parsed.y === 1 ? noun.replace(/s$/, '') : noun;
                                return ctx.parsed.y + ' ' + label;
                            }
                        }
                    }
                },
                scales: {
                    y: {
                        beginAtZero: true,
                        ticks: { precision: 0 },
                        title: { display: true, text: datasetLabel }
                    }
                }
            }
        });
    }

    if (typeof Chart === 'undefined') {
        console.error('Dashboard: Chart.js failed to load (CDN blocked or offline).');
        const msg = 'Charts could not load. Check your internet connection and refresh.';
        ['jobTicketStatusMessage', 'jobsOverTimeMessage', 'officeTaskStatusMessage', 'officeTasksOverTimeMessage']
            .forEach(id => showMessage(id, msg));
        return;
    }

    // ---- Office Task charts ----
    const officeTaskPayload = parsePayload('officeTaskChartData',
        ['officeTaskStatusMessage', 'officeTasksOverTimeMessage']);

    if (officeTaskPayload) {
        renderStatusPie(
            officeTaskPayload.officeTaskStatusOverview || [],
            'officeTaskStatusChart', 'officeTaskStatusMessage', 'tasks');
        renderOverTimeLine(
            officeTaskPayload.officeTasksOverTime || [],
            'officeTasksOverTimeChart', 'officeTasksOverTimeMessage', 'Office Tasks Created', 'tasks');
    }

    // ---- Job Ticket charts ----
    const jobTicketPayload = parsePayload('dashboardChartData',
        ['jobTicketStatusMessage', 'jobsOverTimeMessage']);

    if (jobTicketPayload) {
        renderStatusPie(
            jobTicketPayload.jobTicketStatusOverview || [],
            'jobTicketStatusChart', 'jobTicketStatusMessage', 'tickets');
        renderOverTimeLine(
            jobTicketPayload.jobsOverTime || [],
            'jobsOverTimeChart', 'jobsOverTimeMessage', 'Job Tickets Created', 'tickets');
    }
})();
