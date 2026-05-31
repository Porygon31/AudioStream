namespace AudioStream;

/// <summary>
/// Genere la page web qui se connecte au flux audio local.
/// </summary>
public static class ClientPage
{
    /// <summary>
    /// Retourne le HTML autonome du lecteur audio.
    /// </summary>
    public static string Render(int sampleRate)
    {
        return $$"""
<!doctype html>
<html lang="fr">
<head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1">
    <title>AudioStream</title>
    <style>
        :root {
            color-scheme: light dark;
            font-family: Arial, Helvetica, sans-serif;
            background: #101418;
            color: #f5f7fa;
        }

        body {
            margin: 0;
            min-height: 100vh;
            display: grid;
            place-items: center;
            padding: 24px;
        }

        main {
            width: min(520px, 100%);
            border: 1px solid #3d4652;
            border-radius: 8px;
            padding: 24px;
            background: #182029;
        }

        h1 {
            margin: 0 0 8px;
            font-size: 28px;
            letter-spacing: 0;
        }

        p {
            margin: 0 0 20px;
            color: #c9d3df;
            line-height: 1.45;
        }

        button {
            width: 100%;
            min-height: 48px;
            border: 0;
            border-radius: 6px;
            background: #23c483;
            color: #0a1912;
            cursor: pointer;
            font-size: 16px;
            font-weight: 700;
        }

        button:disabled {
            cursor: wait;
            opacity: .65;
        }

        #status {
            margin-top: 16px;
            padding: 12px;
            border-radius: 6px;
            background: #101418;
            color: #d8e2ec;
            min-height: 20px;
        }
    </style>
</head>
<body>
    <main>
        <h1>AudioStream</h1>
        <p>Ouvre cette page depuis un appareil du reseau local, puis lance la lecture pour ecouter le son du PC.</p>
        <button id="start" type="button">Demarrer l'ecoute</button>
        <div id="status">Pret.</div>
    </main>

    <script>
        // Le serveur envoie des echantillons float32 stereo interleaves a cette frequence.
        const STREAM_SAMPLE_RATE = {{sampleRate}};
        const startButton = document.querySelector("#start");
        const statusElement = document.querySelector("#status");

        let audioContext;
        let workletNode;
        let scriptNode;
        let audioSink;
        let socket;

        function setStatus(message) {
            statusElement.textContent = message;
        }

        function readStereoSample(queueState, left, right, index) {
            if (queueState.queue.length === 0) {
                left[index] = 0;
                right[index] = 0;
                return;
            }

            const packet = queueState.queue[0];
            left[index] = packet[queueState.offset] || 0;
            right[index] = packet[queueState.offset + 1] || 0;
            queueState.offset += 2;

            if (queueState.offset >= packet.length) {
                queueState.queue.shift();
                queueState.offset = 0;
            }
        }

        async function createAudioNode() {
            audioContext = new AudioContext({ sampleRate: STREAM_SAMPLE_RATE });

            if (!audioContext.audioWorklet || typeof AudioWorkletNode === "undefined") {
                createScriptProcessorFallback();
                return;
            }

            // L'AudioWorklet garde une file de petits paquets PCM recus par WebSocket.
            const processorSource = `
                class PcmPlayerProcessor extends AudioWorkletProcessor {
                    constructor(options) {
                        super();
                        this.queue = [];
                        this.offset = 0;
                        this.port.onmessage = (event) => this.queue.push(event.data);
                    }

                    process(inputs, outputs) {
                        const left = outputs[0][0];
                        const right = outputs[0][1] || left;

                        for (let index = 0; index < left.length; index++) {
                            if (this.queue.length === 0) {
                                left[index] = 0;
                                right[index] = 0;
                                continue;
                            }

                            const packet = this.queue[0];
                            left[index] = packet[this.offset] || 0;
                            right[index] = packet[this.offset + 1] || 0;
                            this.offset += 2;

                            if (this.offset >= packet.length) {
                                this.queue.shift();
                                this.offset = 0;
                            }
                        }

                        return true;
                    }
                }

                registerProcessor("pcm-player", PcmPlayerProcessor);
            `;

            const processorUrl = URL.createObjectURL(new Blob([processorSource], { type: "text/javascript" }));
            try {
                await audioContext.audioWorklet.addModule(processorUrl);
            } finally {
                URL.revokeObjectURL(processorUrl);
            }

            workletNode = new AudioWorkletNode(audioContext, "pcm-player", {
                numberOfOutputs: 1,
                outputChannelCount: [2]
            });
            workletNode.connect(audioContext.destination);

            audioSink = {
                postPacket(packet, transferableBuffer) {
                    // Le transfert evite une copie inutile entre le thread reseau et l'AudioWorklet.
                    workletNode.port.postMessage(packet, [transferableBuffer]);
                }
            };
        }

        function createScriptProcessorFallback() {
            // Fallback utile sur HTTP LAN, ou AudioWorklet n'est pas toujours disponible.
            const queueState = { queue: [], offset: 0 };
            scriptNode = audioContext.createScriptProcessor(4096, 0, 2);

            scriptNode.onaudioprocess = (event) => {
                const left = event.outputBuffer.getChannelData(0);
                const right = event.outputBuffer.getChannelData(1);

                for (let index = 0; index < left.length; index++) {
                    readStereoSample(queueState, left, right, index);
                }
            };

            scriptNode.connect(audioContext.destination);
            audioSink = {
                postPacket(packet) {
                    queueState.queue.push(packet);
                }
            };
        }

        async function start() {
            startButton.disabled = true;
            setStatus("Initialisation audio...");

            try {
                await createAudioNode();

                const protocol = location.protocol === "https:" ? "wss:" : "ws:";
                socket = new WebSocket(`${protocol}//${location.host}/audio`);
                socket.binaryType = "arraybuffer";

                socket.onopen = async () => {
                    await audioContext.resume();
                    setStatus("Connecte. Lecture en cours.");
                };

                socket.onmessage = (event) => {
                    audioSink.postPacket(new Float32Array(event.data), event.data);
                };

                socket.onclose = () => {
                    setStatus("Connexion fermee.");
                    startButton.disabled = false;
                };

                socket.onerror = () => {
                    setStatus("Erreur de connexion au flux audio.");
                    startButton.disabled = false;
                };
            } catch (error) {
                setStatus(`Erreur: ${error.message}`);
                startButton.disabled = false;
            }
        }

        startButton.addEventListener("click", start);
    </script>
</body>
</html>
""";
    }
}
