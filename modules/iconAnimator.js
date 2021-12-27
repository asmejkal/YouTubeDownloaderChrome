export default class IconAnimator {
	#currentFrame;
	#timeout;
	#currentJobId;
	#run;
	
	constructor(frameImagesFolder, extension, frameCount, baseIconPath, frequency, timeout) {
		this.frameImagesFolder = frameImagesFolder;
		this.frameCount = frameCount;
		this.baseIconPath = baseIconPath;
		this.extension = extension;
		this.frequency = frequency;
		this.timeout = timeout;
		
		this.#currentFrame = 0;
		this.#timeout = 0;
		this.#currentJobId = 0;
		this.#run = false;
	}
	
	start()
	{
		this.#currentJobId++;
		console.log(`Starting rotate job ${this.#currentJobId}`);
		this.#timeout = Date.now() + this.timeout;
		this.#run = true;
		this.#rotateLoop();
		
		return this.#currentJobId;
	}
	
	stop(jobId)
	{
		console.log(`Stopping rotate job ${jobId}, current job is ${this.#currentJobId}`);
		if (this.#currentJobId == jobId)
			this.#run = false;
	}
	
	#rotateLoop()
	{
		if (Date.now() > this.#timeout)
			this.#run = false;

		if (this.#run)
		{
			chrome.action.setIcon({ path: this.frameImagesFolder + "/" + this.#currentFrame + "." + this.extension});
			this.#currentFrame++;
			if (this.#currentFrame >= this.frameCount) {
			  this.#currentFrame = 0;
			};
			
			setTimeout(this.#rotateLoop.bind(this), this.frequency);
		}
		else
		{
			chrome.action.setIcon({ path: this.baseIconPath });
		}
	}
}