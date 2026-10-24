import { useEffect, useState } from 'react';
import './App.css';

/**
 * @file App.jsx
 * @brief Root React component of the Staff/HR client.
 */

/**
 * @brief Page that shows the forecasts served by the API in a table.
 *
 * On mount it requests `weatherforecast` from the server. While no data has arrived, or the
 * server answered with an error, it shows a loading message instead of the table.
 * Sample template code; it will be replaced by the Staff/HR screens.
 *
 * @returns {JSX.Element} The page heading and either the loading message or the forecast table.
 */
function App() {
    const [forecasts, setForecasts] = useState();

    useEffect(() => {
        populateWeatherData();
    }, []);

    const contents = forecasts === undefined
        ? <p><em>Loading... Please refresh once the ASP.NET backend has started. See <a href="https://aka.ms/jspsintegrationreact">https://aka.ms/jspsintegrationreact</a> for more details.</em></p>
        : <table className="table table-striped" aria-labelledby="tableLabel">
            <thead>
                <tr>
                    <th>Date</th>
                    <th>Temp. (C)</th>
                    <th>Temp. (F)</th>
                    <th>Summary</th>
                </tr>
            </thead>
            <tbody>
                {forecasts.map(forecast =>
                    <tr key={forecast.date}>
                        <td>{forecast.date}</td>
                        <td>{forecast.temperatureC}</td>
                        <td>{forecast.temperatureF}</td>
                        <td>{forecast.summary}</td>
                    </tr>
                )}
            </tbody>
        </table>;

    return (
        <div>
            <h1 id="tableLabel">Weather forecast</h1>
            <p>This component demonstrates fetching data from the server.</p>
            {contents}
        </div>
    );
    
    /**
     * @brief Fetches the forecasts from the API and stores them in state.
     *
     * Does nothing when the response is not OK. Network failures are not caught.
     */
    async function populateWeatherData() {
        const response = await fetch('weatherforecast');
        if (response.ok) {
            const data = await response.json();
            setForecasts(data);
        }
    }
}

export default App;