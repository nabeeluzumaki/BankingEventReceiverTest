import logging
import json
import pyodbc
import azure.functions as func

# Connection string for SQL Server, adjusted based on the provided C# DbContext
CONNECTION_STRING = "Driver={ODBC Driver 17 for SQL Server};Server=localhost\\SQLEXPRESS;Database=BankingApiTest;UID=YOUR_USERNAME;PWD=YOUR_PASSWORD;Encrypt=yes;TrustServerCertificate=no;Connection Timeout=30;"

def main(msg: func.ServiceBusMessage):
    logging.info(f"Received message: {msg.get_body().decode('utf-8')}")
    
    # Parse the message body
    message_body = json.loads(msg.get_body().decode('utf-8'))
    message_type = message_body.get("MessageType")
    account_number = message_body.get("AccountNumber")
    amount = float(message_body.get("Amount"))

    try:
        # Connect to the SQL Database
        with pyodbc.connect(CONNECTION_STRING) as conn:
            cursor = conn.cursor()

            # Fetch the current balance
            cursor.execute("SELECT Balance FROM BankAccounts WHERE AccountNumber = ?", account_number)
            result = cursor.fetchone()

            if not result:
                logging.error(f"Account {account_number} not found.")
                return
            
            balance = result[0]

            # Update the balance based on the message type
            if message_type == "Credit":
                new_balance = balance + amount
            elif message_type == "Debit":
                new_balance = balance - amount
            else:
                logging.error(f"Invalid message type: {message_type}.")
                return

            # Update the database
            cursor.execute("UPDATE BankAccounts SET Balance = ? WHERE AccountNumber = ?", new_balance, account_number)
            conn.commit()

            logging.info(f"Updated balance for {account_number}: {new_balance}")

    except Exception as e:
        logging.error(f"Error processing message: {str(e)}")
