export const contentDispositionFileName = (
  response: Response
): string | null => {
  const header = response.headers.get("Content-Disposition");
  if (!header) return null;

  const extended = /filename\*\s*=\s*[^']*'[^']*'([^;]+)/i.exec(header);
  if (extended?.[1]) {
    try {
      return decodeURIComponent(extended[1].trim());
    } catch {
    }
  }

  const plain = /filename\s*=\s*("([^"]*)"|([^;]+))/i.exec(header);
  const name = (plain?.[2] ?? plain?.[3])?.trim();
  return name ? name : null;
};

export const saveBlob = (blob: Blob, fileName: string): void => {
  const url = URL.createObjectURL(blob);
  const anchor = document.createElement("a");
  anchor.href = url;
  anchor.download = fileName;
  document.body.appendChild(anchor);
  anchor.click();
  anchor.remove();
  URL.revokeObjectURL(url);
};

export const saveResponseAsFile = async (
  response: Response,
  fallbackFileName: string
): Promise<void> => {
  const blob = await response.blob();
  saveBlob(blob, contentDispositionFileName(response) ?? fallbackFileName);
};
