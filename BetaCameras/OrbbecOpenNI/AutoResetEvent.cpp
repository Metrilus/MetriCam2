#include "AutoResetEvent.h"
#include <mutex>
#include <condition_variable>
#include <thread>

bool flag_ = false;
std::mutex protect_;
std::condition_variable signal_;

void AutoResetEventSet()
{
	std::lock_guard<std::mutex> _(protect_);
	flag_ = true;
	signal_.notify_one();
}

void AutoResetEventReset()
{
	std::lock_guard<std::mutex> _(protect_);
	flag_ = false;
}

bool AutoResetEventWaitOne()
{
	std::unique_lock<std::mutex> lk(protect_);
	while (!flag_) // prevent spurious wakeups from doing harm
		signal_.wait(lk);
	flag_ = false; // waiting resets the flag
	return true;
}
