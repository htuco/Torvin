/// Za `retry:` providera. Riverpod 3 sam ponavlja provider koji padne, a ovdje se greška odmah prikaže, dok je
/// ponovni pokušaj dugme na ekranu.
Duration? noRetry(int _, Object _) => null;
