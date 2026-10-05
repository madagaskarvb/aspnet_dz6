// SecureTodo — AJAX-действия с antiforgery-токеном

document.addEventListener('DOMContentLoaded', () => {
    const csrfMeta = document.querySelector('meta[name="csrf-token"]');
    const csrfToken = csrfMeta ? csrfMeta.getAttribute('content') : null;

    // Кнопка "Отметить выполненной"
    document.querySelectorAll('[data-mark-completed]').forEach(btn => {
        btn.addEventListener('click', async (e) => {
            e.preventDefault();
            const id = btn.getAttribute('data-id');
            if (!id) return;

            const row = document.querySelector(`[data-task-row="${id}"]`);
            const statusCell = row ? row.querySelector('.status-cell') : null;

            try {
                const resp = await fetch(`/Tasks?handler=MarkCompleted&id=${id}`, {
                    method: 'POST',
                    headers: {
                        'X-CSRF-TOKEN': csrfToken || '',
                        'Accept': 'application/json'
                    }
                });

                if (!resp.ok) {
                    alert('Ошибка: ' + resp.status);
                    return;
                }

                const data = await resp.json();
                if (statusCell) {
                    statusCell.textContent = data.isCompleted ? 'Выполнено' : 'В работе';
                }
                btn.disabled = true;
            } catch (err) {
                console.error(err);
                alert('Ошибка сети');
            }
        });
    });
});