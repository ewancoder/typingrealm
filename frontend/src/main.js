import { initializeTypingState } from './typing.js';
import { createReplay } from './replay.js';
import { notifier } from './notifier.js';
import { initializeSessions } from './typing-sessions.js';
import { auth } from './auth.js';
import { http } from './http.js';
import { config } from './config.js';
import { processWelcomeScreen, play, playCountdown } from './welcome.js';
import './google-auth.js'; // Sets up google auth.

// Initialize authentication.
await auth.getToken();
document.body.classList.remove('non-scrollable');

await processWelcomeScreen();

let typing = undefined;
let replay = undefined;
let sessions = undefined;
let customTheme = undefined;

// Typing manager that manager how the text is being typed.
const textElement = document.getElementById('text');
const replayElement = document.getElementById('replay');
const progressElement = document.getElementById('progress-value');
const typingState = initializeTypingState(textElement, async data => {
    if (customTheme !== undefined && customTheme.length > 0) {
        data.textMetadata = {
            theme: customTheme
        }
    }

    uploadAndUpdateStatus(data); // Intentionally not awaited for faster UI experience.

    showControls();

    replay.replayTypingSession(data.text, data.events);

    play('Nice! Keep typing and gathering statistics. We\'ll keep showing you tips here.', 1);
    localStorage.setItem('tips', 2);
});

async function uploadAndUpdateStatus(data) {
    await sessions.uploadResults(data);
    await updateStatus();
}

const countdownElement = document.getElementById('countdown');

const replayTypingState = initializeTypingState(replayElement);

// Replay managers for processing controls (key presses).
typing = createReplay(typingState);
replay = createReplay(replayTypingState);

// Sessions manager for reviewing and deleting previous sessions.
const sessionsElement = document.getElementById('sessions');
sessions = await initializeSessions(replay, sessionsElement, raceGhost);

// Input area for creating custom text to type.
const inputAreaElement = document.getElementById('input-area');
const inputElement = document.getElementById('input');
showControls();

play('Copy text that you want to type here, or use the Generate button so we can generate it for you.', 1);

/* Function that is being called on submit button click, when you submit
 * the text that you want to type. */
window.submitText = async function submitText() {
    if (inputElement.value.trim() === '') {
        notifier.alertError('Please enter non-empty text to start.');
        return;
    }

    // If text is too long.
    if (inputElement.value.length > 10000) {
        notifier.alertError('Text is too long.');
        return;
    }

    // In future we might generate texts on the server side.
    const text = await getNextText();

    typingState.prepareText(text);

    replay.stop();

    hideControls();

    play('Now type this text as fast as you can, correcting any mistakes that you make. Complete telemetry of all your keystrokes and their delays will be recorded.', 1);
}

window.generateText = async function generateText() {
    const romanized = false;
    let text = null;
    try {
        customTheme = inputElement.value.trim();
        let content = await http.get(`${config.textApiUrl}/generate?length=300&theme=${customTheme}&romanized=${romanized}`);
        text = romanized ? await content.json() : await content.text(); // use json() here when requesting romanized text.
    } catch {
        notifier.alertError('Could not generate a new text.');
        return;
    }
    if (text === null || text === undefined || text == '') {
        notifier.alertError('Could not generate a new text.');
        return;
    }

    typingState.prepareText(text);

    replay.stop();

    hideControls();

    play('Now type this text as fast as you can, correcting any mistakes that you make. Complete telemetry of all your keystrokes and their delays will be recorded.', 1);
}

// TODO: Handle errors / success with notifications.
window.setDailyGoal = async function setDailyGoal() {
    const goalValue = prompt('Enter your daily goal in characters (one default text = 300 characters):');
    if (!goalValue) return;
    await http.post(`${config.statisticsApiUrl}/profile`, {
        goalCharacters: goalValue
    });
    await updateStatus();
}

async function updateStatus() {
    const profileResponse = await http.get(`${config.statisticsApiUrl}/profile`);
    const profile = await profileResponse.json();
    notifier.alertSuccess('Goal status: ' + profile.typedToday + ' / ' + profile.goalCharacters);

    let progress = 0;
    if (profile.goalCharacters != 0) {
        progress = 100 * profile.typedToday / profile.goalCharacters;
        if (progress > 100) progress = 100;
    }

    if (progress === 100) {
        progressElement.style.backgroundColor = '#09e';
    } else {
        progressElement.style.backgroundColor = '#0a0';
    }

    progressElement.style.width = `${progress}%`;
}
updateStatus(); // Intentionally not awaited.

async function raceGhost(loadedSession) {
    replay.stop();
    inputAreaElement.classList.add('hidden');
    replayElement.classList.remove('hidden');
    textElement.classList.remove('hidden');
    sessions.hide();

    typingState.prepareText(loadedSession.text);

    await playCountdown();

    document.addEventListener('keydown', typing.processKeyDown);
    document.addEventListener('keyup', typing.processKeyUp);
    replay.replayTypingSession(loadedSession.text, loadedSession.events);
}

// Hides input field and sessions table, leaving only text to type.
function hideControls() {
    inputAreaElement.classList.add('hidden');
    replayElement.classList.add('hidden');
    textElement.classList.remove('hidden');
    sessions.hide();

    document.addEventListener('keydown', typing.processKeyDown);
    document.addEventListener('keyup', typing.processKeyUp);
}

// Shows input field and sessions table.
function showControls() {
    inputAreaElement.classList.remove('hidden');
    replayElement.classList.remove('hidden');
    textElement.classList.add('hidden');
    sessions.show();

    document.removeEventListener('keydown', typing.processKeyDown);
    document.removeEventListener('keyup', typing.processKeyUp);
}

// Gets next text. Currently reads input field, we'll generate texts on the
// server side in future.
function getNextText() {
    const text = inputElement.value;
    inputElement.value = '';
    return text;
}
