import { act, render, screen, within } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import App from './App.jsx'

const forecasts = [
    { date: '2026-10-22', temperatureC: 0, temperatureF: 32, summary: 'Freezing' },
    { date: '2026-10-23', temperatureC: 25, temperatureF: 76, summary: 'Warm' },
    { date: '2026-10-24', temperatureC: 40, temperatureF: 103, summary: 'Hot' },
]

function stubFetch(response) {
    const fetchMock = vi.fn().mockResolvedValue(response)
    vi.stubGlobal('fetch', fetchMock)
    return fetchMock
}

describe('App', () => {
    it('shows the page heading', async () => {
        stubFetch({ ok: true, json: async () => [] })

        render(<App />)
        await screen.findByRole('table')

        expect(screen.getByRole('heading', { name: 'Weather forecast' })).toBeInTheDocument()
    })

    it('shows the loading message until the forecasts arrive', () => {
        vi.stubGlobal('fetch', vi.fn(() => new Promise(() => {})))

        render(<App />)

        expect(screen.getByText(/Loading/)).toBeInTheDocument()
        expect(screen.queryByRole('table')).not.toBeInTheDocument()
    })

    it('requests the forecasts from the weatherforecast endpoint', async () => {
        const fetchMock = stubFetch({ ok: true, json: async () => forecasts })

        render(<App />)
        await screen.findByRole('table')

        expect(fetchMock).toHaveBeenCalledTimes(1)
        expect(fetchMock).toHaveBeenCalledWith('weatherforecast')
    })

    it('renders one table row per forecast with every field', async () => {
        stubFetch({ ok: true, json: async () => forecasts })

        render(<App />)
        const table = await screen.findByRole('table')

        const rows = within(table).getAllByRole('row').slice(1)
        expect(rows).toHaveLength(forecasts.length)
        forecasts.forEach((forecast, index) => {
            const cells = within(rows[index]).getAllByRole('cell').map(cell => cell.textContent)
            expect(cells).toEqual([
                forecast.date,
                String(forecast.temperatureC),
                String(forecast.temperatureF),
                forecast.summary,
            ])
        })
    })

    it('renders the table column headers', async () => {
        stubFetch({ ok: true, json: async () => forecasts })

        render(<App />)
        const table = await screen.findByRole('table')

        const headers = within(table).getAllByRole('columnheader').map(header => header.textContent)
        expect(headers).toEqual(['Date', 'Temp. (C)', 'Temp. (F)', 'Summary'])
        expect(screen.queryByText(/Loading/)).not.toBeInTheDocument()
    })

    it('keeps showing the loading message when the server responds with an error', async () => {
        const json = vi.fn().mockResolvedValue({})
        const fetchMock = stubFetch({ ok: false, status: 500, json })

        await act(async () => {
            render(<App />)
        })

        expect(fetchMock).toHaveBeenCalledTimes(1)
        expect(json).not.toHaveBeenCalled()
        expect(screen.getByText(/Loading/)).toBeInTheDocument()
        expect(screen.queryByRole('table')).not.toBeInTheDocument()
    })
})
