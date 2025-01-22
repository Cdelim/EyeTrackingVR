from flask import Flask, request, jsonify

app = Flask(__name__)

@app.route('/calculate', methods=['POST'])
def calculate():
    data = request.json  # Receive data from Unity
    value1 = data.get('value1')
    value2 = data.get('value2')

    # Perform your calculation
    result = value1 + value2  # Example calculation

    return jsonify({'result': result})  # Send result back to Unity

if __name__ == '__main__':
    app.run(host='0.0.0.0', port=5000)  # Run on localhost
