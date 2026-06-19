document.addEventListener("DOMContentLoaded", function () {
    const chatRoot = document.getElementById("eventChatRoot");

    if (!chatRoot) {
        return;
    }

    const eventId = parseInt(chatRoot.dataset.eventId);
    const isAdmin = chatRoot.dataset.isAdmin === "true";
    const currentUserId = chatRoot.dataset.currentUserId || "";
    const canReportMessages = chatRoot.dataset.canReportMessages === "true";
    const returnUrl = chatRoot.dataset.returnUrl || window.location.pathname + window.location.search;

    const messagesContainer = document.getElementById("eventChatMessages");
    const chatForm = document.getElementById("eventChatForm");
    const chatInput = document.getElementById("eventChatInput");
    const errorBox = document.getElementById("eventChatError");

    if (!eventId || !messagesContainer || !chatForm || !chatInput) {
        return;
    }

    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/hubs/eventChat")
        .withAutomaticReconnect()
        .build();

    connection.on("ReceiveMessage", function (message) {
        appendMessage(message);
        scrollChatToBottom();
    });

    connection.on("MessageDeleted", function (messageId) {
        markMessageAsDeleted(messageId);
    });

    connection.start()
        .then(function () {
            return connection.invoke("JoinEvent", eventId);
        })
        .then(function () {
            scrollChatToBottom();
        })
        .catch(function (error) {
            showError(error.message || "Не удалось подключиться к чату");
        });

    chatForm.addEventListener("submit", function (event) {
        event.preventDefault();

        const text = chatInput.value.trim();

        if (!text) {
            showError("Сообщение не может быть пустым");
            return;
        }

        if (text.length > 1000) {
            showError("Сообщение не должно превышать 1000 символов");
            return;
        }

        hideError();

        connection.invoke("SendMessage", eventId, text)
            .then(function () {
                chatInput.value = "";
            })
            .catch(function (error) {
                showError(error.message || "Не удалось отправить сообщение");
            });
    });

    messagesContainer.addEventListener("click", function (event) {
        const button = event.target.closest(".event-chat-delete-button");

        if (!button) {
            return;
        }

        const messageId = parseInt(button.dataset.messageId);

        if (!messageId) {
            return;
        }

        if (!confirm("Удалить сообщение?")) {
            return;
        }

        connection.invoke("DeleteMessage", messageId)
            .catch(function (error) {
                showError(error.message || "Не удалось удалить сообщение");
            });
    });

    function appendMessage(message) {
        const messageElement = document.createElement("div");
        messageElement.className = "event-chat-message";
        messageElement.dataset.messageId = message.id;

        const avatar = document.createElement("img");
        avatar.className = "event-chat-avatar";
        avatar.alt = "Аватар";
        avatar.src = message.authorAvatarPath || "/images/default-avatar.png";

        const body = document.createElement("div");
        body.className = "event-chat-message-body";

        const header = document.createElement("div");
        header.className = "event-chat-message-header";

        const author = document.createElement("strong");
        author.textContent = message.authorName;

        const date = document.createElement("span");
        date.textContent = message.createdAt;

        header.appendChild(author);
        header.appendChild(date);

        const text = document.createElement("div");
        text.className = "event-chat-message-text";
        text.textContent = message.text;

        body.appendChild(header);
        body.appendChild(text);

        const actions = document.createElement("div");
        actions.className = "event-chat-actions";

        if (canReportMessages && message.userId !== currentUserId) {
            const reportLink = document.createElement("a");
            reportLink.className = "btn btn-sm btn-outline-danger";
            reportLink.href = `/Reports/CreateForMessage/${message.id}?returnUrl=${encodeURIComponent(returnUrl)}`;
            reportLink.textContent = "Пожаловаться";

            actions.appendChild(reportLink);
        }

        if (isAdmin) {
            const deleteButton = document.createElement("button");
            deleteButton.type = "button";
            deleteButton.className = "btn btn-sm btn-outline-danger event-chat-delete-button";
            deleteButton.dataset.messageId = message.id;
            deleteButton.textContent = "Удалить";

            actions.appendChild(deleteButton);
        }

        messageElement.appendChild(avatar);
        messageElement.appendChild(body);
        messageElement.appendChild(actions);

        messagesContainer.appendChild(messageElement);
    }

    function markMessageAsDeleted(messageId) {
        const messageElement = messagesContainer.querySelector(`[data-message-id="${messageId}"]`);

        if (!messageElement) {
            return;
        }

        messageElement.classList.add("event-chat-message-deleted");

        const textElement = messageElement.querySelector(".event-chat-message-text");

        if (textElement) {
            textElement.innerHTML = "";
            const deletedText = document.createElement("em");
            deletedText.textContent = "Сообщение удалено администрацией";
            textElement.appendChild(deletedText);
        }

        const actionsElement = messageElement.querySelector(".event-chat-actions");

        if (actionsElement) {
            actionsElement.innerHTML = "";
        }
    }

    function scrollChatToBottom() {
        messagesContainer.scrollTop = messagesContainer.scrollHeight;
    }

    function showError(message) {
        if (!errorBox) {
            return;
        }

        errorBox.textContent = message;
        errorBox.classList.remove("d-none");
    }

    function hideError() {
        if (!errorBox) {
            return;
        }

        errorBox.textContent = "";
        errorBox.classList.add("d-none");
    }
});