/**
 * @file useApi.js
 * @brief React hook that loads data from the Staff/HR API.
 */
import { useCallback, useEffect, useState } from 'react';

/**
 * Runs `load` whenever one of `deps` changes and returns { data, error, loading, reload }.
 * While a new request is in flight the previous data stays on screen, so tables don't flash.
 * `deps` must be JSON-serialisable values (strings, numbers, plain objects).
 */
export function useApi(load, deps) {
  const [version, setVersion] = useState(0);
  const key = JSON.stringify(deps) + '#' + version;
  const [result, setResult] = useState({ key: null, data: undefined, error: null });

  useEffect(() => {
    let live = true;
    Promise.resolve()
      .then(load)
      .then(
        (data) => live && setResult({ key, data, error: null }),
        (error) => live && setResult((previous) => ({ key, data: previous.data, error })),
      );
    return () => { live = false; };
    // `load` is a new closure each render; `key` captures everything it depends on.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [key]);

  const reload = useCallback(() => setVersion((v) => v + 1), []);
  const current = result.key === key;
  return { data: result.data, error: current ? result.error : null, loading: !current, reload };
}
