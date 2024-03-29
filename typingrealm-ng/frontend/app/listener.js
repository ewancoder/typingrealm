/* global humanizeDuration */

import retrieveText from './text.js';
import submitTypingResult, { getProfileAndGlobalStatistics } from './submit-typing-result.js';
import Typer from './typer.js';

const textElement = document.getElementById('text');
const statisticsElement = document.getElementById('statistics');

let typer = null;

export default async function listen() {
    const stats = await getProfileAndGlobalStatistics();

    showStatistics(stats);

    document.addEventListener('keydown', processKeyDown);
    document.addEventListener('keyup', processKeyUp);
}

async function processKeyDown(event) {
    if (typer && !typer.isFinished) {
        typer.type(event);
        return;
    }

    if (event.key === 'Enter') {
        // Hide statistics.
        statisticsElement.classList.add('hidden');

        // TODO: Show loader if loading takes too much time.
        // But for now we have lightning-fast cache, disable the loader.
        /* textElement.classList.remove('hidden');
        textElement.innerHTML = '<div class="loader"></div>'; */

        // Retrieve next text from the API.
        const text = await retrieveText();

        // Mock loading for viewing loader animation.
        // await new Promise(r => setTimeout(r, 2000));

        // Create new Typer from this text, set up callback to log the Typer
        // results when we finish typing.
        // Assign it to an intermediatory variable so that we don't start
        // handling keyboard input yet.
        const localTyper = new Typer(text, async t => {
            const statisticsResult = await submitTypingResult(t);
            showStatistics(statisticsResult);
        });

        // Remove loader and add Typer characters to the text element.
        textElement.innerHTML = '';
        textElement.append(...localTyper.characterSpans);
        textElement.classList.remove('hidden');

        // Set the new typer so it can start accepting keyboard input.
        typer = localTyper;
    }
}

function showStatistics(statisticsResult) {
    textElement.classList.add('hidden');

    statisticsElement.innerHTML = '';

    // TODO: Fill statistics element with data.
    if (statisticsResult.current) {
        addAnalyticsEntry('Total characters typed', statisticsResult.current.totalCharactersCount);
        addAnalyticsEntry('Total errors', statisticsResult.current.errorCharactersCount);
        addAnalyticsEntry('Total time taken', `${humanizeDuration(Math.round(statisticsResult.current.totalTimeMs / 100) * 100)}`);
        addAnalyticsEntry('Average speed', `${statisticsResult.current.speedWpm.toFixed(2)} WPM`);
        addAnalyticsEntry('Precision', `${statisticsResult.current.precision.toFixed(2)} %`);
    }

    statisticsElement.innerHTML += '<div class="statistics-entry" style="margin-top: 50px">Press ENTER to start typing next text</div>';

    // TODO: Also don't show all-users statistics if there is none at all.
    if (statisticsResult.allTime.speedWpm) {
        statisticsElement.innerHTML += '<div class="statistics-entry" style="margin-bottom: 50px; font-size: 1.5rem">Below is YOUR all-time statistics</div>';
        addAnalyticsEntry('Total characters typed', statisticsResult.allTime.totalCharactersCount);
        addAnalyticsEntry('Total errors', statisticsResult.allTime.errorCharactersCount);
        addAnalyticsEntry('Total time taken', `${humanizeDuration(Math.round(statisticsResult.allTime.totalTimeMs / 100) * 100)}`);
        addAnalyticsEntry('Average speed', `${statisticsResult.allTime.speedWpm.toFixed(2)} WPM`);
        addAnalyticsEntry('Precision', `${statisticsResult.allTime.precision.toFixed(2)} %`);
    }
    statisticsElement.innerHTML += '<div class="statistics-entry" style="margin: 100px; font-size: 1.5rem">Below is the global statistics for ALL users</div>';
    addAnalyticsEntry('Total characters typed', statisticsResult.global.totalCharactersCount);
    addAnalyticsEntry('Total errors', statisticsResult.global.errorCharactersCount);
    addAnalyticsEntry('Total time taken', `${humanizeDuration(Math.round(statisticsResult.global.totalTimeMs / 100) * 100)}`);
    addAnalyticsEntry('Average speed', `${statisticsResult.global.speedWpm.toFixed(2)} WPM`);
    addAnalyticsEntry('Precision', `${statisticsResult.global.precision.toFixed(2)} %`);

    function addAnalyticsEntry(name, value) {
        const element = document.createElement('div');
        element.classList.add('statistics-entry');

        const nameElement = document.createElement('span');
        nameElement.classList.add('statistics-entry-name');
        nameElement.innerHTML = name;

        const valueElement = document.createElement('span');
        valueElement.classList.add('statistics-entry-value');
        valueElement.innerHTML = value;

        element.appendChild(nameElement);
        element.appendChild(valueElement);

        statisticsElement.appendChild(element);
    }

    statisticsElement.classList.remove('hidden');
}

function processKeyUp(event) {
    if (typer && !typer.isFinished) {
        typer.type(event);
    }
}
