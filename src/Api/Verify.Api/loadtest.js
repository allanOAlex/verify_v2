import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
    vus: 100, // number of virtual users
    duration: '30s', // duration of the load test
};

export default function () {
    const res = http.get('https://localhost:7260/api/dht/fetchaccountinfo'); // Update this to your actual API endpoint
    
    // Check that the response status is 200
    check(res, {
        'is status 200': (r) => r.status === 200,
    });

    sleep(1); // Sleep for 1 second between requests
}
