import pkg from 'pg';

const { Pool } = pkg;

const pool = new Pool({
    host: process.env.DB_HOST || 'localhost',
    port: parseInt(process.env.DB_PORT || '5432'),
    database: process.env.DB_NAME || 'chess_sessions',
    user: process.env.DB_USER || 'postgres',
    password: process.env.DB_PASSWORD || 'postgres',
    connectionTimeoutMillis: 5000,
});

/**
 * Инициализация таблиц с повторными попытками.
 */
export async function initDatabase(retries = 10, delay = 2000) {
    for (let attempt = 1; attempt <= retries; attempt++) {
        try {
            const client = await pool.connect();
            try {
                await client.query(`
                    CREATE TABLE IF NOT EXISTS sessions (
                        id SERIAL PRIMARY KEY,
                        status VARCHAR(20) DEFAULT 'active',
                        fen VARCHAR(100) DEFAULT 'rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1',
                        moves TEXT[] DEFAULT '{}',
                        result VARCHAR(20),
                        duration INTEGER,
                        created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
                        completed_at TIMESTAMP WITH TIME ZONE
                    );
                `);
                console.log('Таблица sessions готова');
                return;
            } finally {
                client.release();
            }
        } catch (error) {
            console.error(
                `Попытка ${attempt}/${retries}: БД не готова (${error.message})`
            );
            if (attempt === retries) {
                throw new Error(
                    `Не удалось подключиться к БД после ${retries} попыток`
                );
            }
            await new Promise((resolve) => setTimeout(resolve, delay));
        }
    }
}

export default pool;