-- 空の開発用スキーマ向けの架空データ。初回のみ実行する。
-- 実行ツールはエラー時に停止・ロールバックする設定で使用する。
INSERT INTO BOOK VALUES ('book-a', '図書館サンプルA', 'サンプル著者A', '技術');
INSERT INTO BOOK VALUES ('book-b', '図書館サンプルB', 'サンプル著者B', '技術');
INSERT INTO BOOK VALUES ('book-c', '図書館サンプルC', 'サンプル著者C', '文学');
INSERT INTO BOOK VALUES ('book-d', '図書館サンプルD', 'サンプル著者D', '文学');
INSERT INTO BOOK_COPY VALUES ('copy-001', 'book-a', 'C-001', 'A-01');
INSERT INTO BOOK_COPY VALUES ('copy-002', 'book-a', 'C-002', 'A-01');
INSERT INTO BOOK_COPY VALUES ('copy-003', 'book-b', 'C-003', 'A-02');
INSERT INTO BOOK_COPY VALUES ('copy-004', 'book-c', 'C-004', 'B-01');
INSERT INTO BOOK_COPY VALUES ('copy-005', 'book-d', 'C-005', NULL);
INSERT INTO MEMBER VALUES ('member-a', 'M-001', 'サンプル利用者A');
INSERT INTO MEMBER VALUES ('member-b', 'M-002', 'サンプル利用者B');
INSERT INTO APP_USER VALUES ('staff-a', 'librarian', NULL, 'Librarian', NULL);
INSERT INTO APP_USER VALUES ('user-a', 'member.a', NULL, 'Member', 'member-a');
INSERT INTO APP_USER VALUES ('user-b', 'member.b', NULL, 'Member', 'member-b');
INSERT INTO LOAN VALUES ('loan-001', 'copy-002', 'member-a', DATE '2026-09-01', DATE '2026-09-15', TIMESTAMP '2026-09-01 00:00:00', 'staff-a', TIMESTAMP '2026-09-05 00:00:00', 'staff-a');
INSERT INTO LOAN VALUES ('loan-002', 'copy-002', 'member-a', DATE '2026-10-01', DATE '2026-10-15', TIMESTAMP '2026-10-01 00:00:00', 'staff-a', NULL, NULL);
INSERT INTO LOAN VALUES ('loan-003', 'copy-003', 'member-a', DATE '2026-08-01', DATE '2026-08-15', TIMESTAMP '2026-08-01 00:00:00', 'staff-a', TIMESTAMP '2026-08-10 00:00:00', 'staff-a');
INSERT INTO LOAN VALUES ('loan-004', 'copy-003', 'member-a', DATE '2026-08-20', DATE '2026-09-03', TIMESTAMP '2026-08-20 00:00:00', 'staff-a', TIMESTAMP '2026-08-25 00:00:00', 'staff-a');
INSERT INTO LOAN VALUES ('loan-005', 'copy-004', 'member-b', DATE '2026-09-10', DATE '2026-09-24', TIMESTAMP '2026-09-10 00:00:00', 'staff-a', NULL, NULL);
COMMIT;
