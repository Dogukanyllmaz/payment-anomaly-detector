import { render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import App from './App'
import { report } from './test/fixture'

const loaded = () => Promise.resolve(report)

function findingButtons() {
  return within(screen.getByRole('list', { name: 'Findings' })).getAllByRole('button')
}

describe('App', () => {
  it('says what was checked and how many findings came out', async () => {
    render(<App load={loaded} />)

    expect(await screen.findByRole('heading', { name: '3 findings' })).toBeInTheDocument()
    expect(screen.getByText(/12 invoices and 12 orders/)).toBeInTheDocument()
  })

  it('opens the first finding with the invoices behind it', async () => {
    render(<App load={loaded} />)

    const detail = await screen.findByRole('region', { name: 'Finding detail' })
    expect(within(detail).getByText('SUP-104 billed 18,450.00 twice, 3 day(s) apart')).toBeInTheDocument()
    expect(within(detail).getByText('F-55012')).toBeInTheDocument()
    expect(within(detail).getByText('F-55031')).toBeInTheDocument()
  })

  it('filters the list to one rule and back', async () => {
    const user = userEvent.setup()
    render(<App load={loaded} />)
    const filters = within(await screen.findByRole('group', { name: 'Filter by rule' }))

    await user.click(filters.getByRole('button', { name: /Self-approval/ }))

    expect(findingButtons()).toHaveLength(1)
    expect(filters.getByRole('button', { name: /Self-approval/ })).toHaveAttribute('aria-pressed', 'true')
    expect(screen.getByRole('status')).toHaveTextContent('Showing 1 of 3 findings')
    expect(within(screen.getByRole('region', { name: 'Finding detail' })).getByText('PO-005')).toBeInTheDocument()

    await user.click(filters.getByRole('button', { name: /All rules/ }))
    expect(findingButtons()).toHaveLength(3)
  })

  it('starts from the first visible finding whenever the filter changes', async () => {
    const user = userEvent.setup()
    render(<App load={loaded} />)
    const filters = within(await screen.findByRole('group', { name: 'Filter by rule' }))

    await user.click(findingButtons()[2])
    await user.click(filters.getByRole('button', { name: /Self-approval/ }))
    await user.click(filters.getByRole('button', { name: /All rules/ }))

    expect(findingButtons()[0]).toHaveAttribute('aria-current', 'true')
  })

  it('shows requester, approver and approval time for an order finding', async () => {
    const user = userEvent.setup()
    render(<App load={loaded} />)
    await screen.findByRole('list', { name: 'Findings' })

    await user.click(findingButtons()[2])

    const detail = screen.getByRole('region', { name: 'Finding detail' })
    expect(within(detail).getByText('PO-008')).toBeInTheDocument()
    expect(within(detail).getByText('2026-03-09 22:15')).toBeInTheDocument()
    expect(findingButtons()[2]).toHaveAttribute('aria-current', 'true')
  })

  it('explains a loading failure instead of showing an empty page', async () => {
    render(<App load={() => Promise.reject(new Error('Could not load the audit report (HTTP 404)'))} />)

    expect(await screen.findByRole('alert')).toHaveTextContent('Could not load the audit report (HTTP 404)')
  })
})
