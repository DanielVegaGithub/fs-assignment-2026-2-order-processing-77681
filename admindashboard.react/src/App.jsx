import { useEffect, useState } from 'react'
import './App.css'

function App() {
    const [orders, setOrders] = useState([])
    const [loading, setLoading] = useState(true)
    const [error, setError] = useState('')
    const [selectedWorkflow, setSelectedWorkflow] = useState(null)
    const [workflowLoading, setWorkflowLoading] = useState(false)
    const [statusFilter, setStatusFilter] = useState('All')

    const loadOrders = () => {
        setLoading(true)
        setError('')

        fetch('https://localhost:7041/api/admin/orders')
            .then(response => {
                if (!response.ok) {
                    throw new Error('Error loading orders')
                }
                return response.json()
            })
            .then(data => {
                setOrders(data)
                setLoading(false)
            })
            .catch(() => {
                setError('Could not load orders from API')
                setLoading(false)
            })
    }

    useEffect(() => {
        fetch('https://localhost:7041/api/admin/orders')
            .then(response => {
                if (!response.ok) {
                    throw new Error('Error loading orders')
                }
                return response.json()
            })
            .then(data => {
                setOrders(data)
                setLoading(false)
            })
            .catch(() => {
                setError('Could not load orders from API')
                setLoading(false)
            })
    }, [])

    const loadWorkflow = (orderId) => {
        setWorkflowLoading(true)
        setSelectedWorkflow(null)

        fetch(`https://localhost:7041/api/admin/orders/${orderId}/workflow`)
            .then(response => {
                if (!response.ok) {
                    throw new Error('Error loading workflow')
                }
                return response.json()
            })
            .then(data => {
                setSelectedWorkflow(data)
                setWorkflowLoading(false)
            })
            .catch(() => {
                setWorkflowLoading(false)
            })
    }

    const filteredOrders =
        statusFilter === 'All'
            ? orders
            : orders.filter(order => order.status === statusFilter)

    const statuses = ['All', ...new Set(orders.map(order => order.status))]

    return (
        <div style={{ padding: '2rem', fontFamily: 'Arial, sans-serif', maxWidth: '1200px', margin: '0 auto' }}>
            <h1>Admin Dashboard</h1>
            <p>Order monitoring panel</p>

            <div style={{ display: 'flex', gap: '1rem', alignItems: 'center', marginBottom: '1rem' }}>
                <button onClick={loadOrders}>Refresh Orders</button>

                <label>
                    Filter by status:{' '}
                    <select value={statusFilter} onChange={(e) => setStatusFilter(e.target.value)}>
                        {statuses.map(status => (
                            <option key={status} value={status}>
                                {status}
                            </option>
                        ))}
                    </select>
                </label>
            </div>

            {loading && <p>Loading orders...</p>}
            {error && <p style={{ color: 'red' }}>{error}</p>}

            {!loading && !error && (
                <>
                    <table style={{ width: '100%', borderCollapse: 'collapse', marginTop: '1rem' }}>
                        <thead>
                            <tr>
                                <th style={{ borderBottom: '1px solid #ccc', textAlign: 'left', padding: '0.5rem' }}>Order Id</th>
                                <th style={{ borderBottom: '1px solid #ccc', textAlign: 'left', padding: '0.5rem' }}>Customer Id</th>
                                <th style={{ borderBottom: '1px solid #ccc', textAlign: 'left', padding: '0.5rem' }}>Created At</th>
                                <th style={{ borderBottom: '1px solid #ccc', textAlign: 'left', padding: '0.5rem' }}>Status</th>
                                <th style={{ borderBottom: '1px solid #ccc', textAlign: 'left', padding: '0.5rem' }}>Total</th>
                                <th style={{ borderBottom: '1px solid #ccc', textAlign: 'left', padding: '0.5rem' }}>Items</th>
                                <th style={{ borderBottom: '1px solid #ccc', textAlign: 'left', padding: '0.5rem' }}>Action</th>
                            </tr>
                        </thead>
                        <tbody>
                            {filteredOrders.map(order => (
                                <tr key={order.id}>
                                    <td style={{ borderBottom: '1px solid #eee', padding: '0.5rem' }}>{order.id}</td>
                                    <td style={{ borderBottom: '1px solid #eee', padding: '0.5rem' }}>{order.customerId}</td>
                                    <td style={{ borderBottom: '1px solid #eee', padding: '0.5rem' }}>{order.createdAt}</td>
                                    <td style={{ borderBottom: '1px solid #eee', padding: '0.5rem' }}>{order.status}</td>
                                    <td style={{ borderBottom: '1px solid #eee', padding: '0.5rem' }}>{order.totalAmount}</td>
                                    <td style={{ borderBottom: '1px solid #eee', padding: '0.5rem' }}>{order.itemCount}</td>
                                    <td style={{ borderBottom: '1px solid #eee', padding: '0.5rem' }}>
                                        <button onClick={() => loadWorkflow(order.id)}>
                                            View Workflow
                                        </button>
                                    </td>
                                </tr>
                            ))}
                        </tbody>
                    </table>

                    {workflowLoading && <p style={{ marginTop: '1rem' }}>Loading workflow...</p>}

                    {selectedWorkflow && (
                        <div style={{ marginTop: '2rem', padding: '1rem', border: '1px solid #ccc', borderRadius: '8px' }}>
                            <h2>Workflow Detail</h2>

                            <p><strong>Order Id:</strong> {selectedWorkflow.order.id}</p>
                            <p><strong>Status:</strong> {selectedWorkflow.order.status}</p>
                            <p><strong>Total:</strong> {selectedWorkflow.order.totalAmount}</p>

                            <h3>Inventory Records</h3>
                            <pre>{JSON.stringify(selectedWorkflow.inventoryRecords, null, 2)}</pre>

                            <h3>Payment Records</h3>
                            <pre>{JSON.stringify(selectedWorkflow.paymentRecords, null, 2)}</pre>

                            <h3>Shipment Records</h3>
                            <pre>{JSON.stringify(selectedWorkflow.shipmentRecords, null, 2)}</pre>
                        </div>
                    )}
                </>
            )}
        </div>
    )
}

export default App